---
name: async-assertion
description: Deterministic async assertions for Orleans tests using per-subscriber channels, a shared grain call filter, and event-driven retry without polling.
---

# Async WaitForAssertion via Grain Call Collection

Use this skill when an Orleans test depends on asynchronous side-effects — RPCs, one-way calls, or stream deliveries — and must not use polling (`Task.Delay` loops) or implementation-detail coupling (specific method names, call counts, etc.).

## Design philosophy: the assertion IS the contract

Tests should express **what** must eventually become true, not **how** the system makes it true. The assertion itself is the contract — the test should never wait for a particular internal RPC sequence.

When you couple tests to implementation details you get:

- **Fragile tests** — refactoring internal call chains breaks tests even though the observable behavior is unchanged.
- **False confidence** — tests assert the wrong thing (a method was called) instead of the right thing (state changed).

This skill treats _any_ grain call to the target grain as a retry trigger. It does not care which method was called or how many calls it took. The assertion alone decides pass/fail.

## When to use

- Tests depend on eventually-consistent state caused by grain-to-grain calls or stream deliveries.
- You need deterministic wait behavior (no `Task.Delay` or configurable poll intervals).
- Tests run in parallel and must not interfere with each other.

## Architecture overview

The skill has two layers:

1. **Framework-agnostic core** — the grain call filter, per-subscriber channels, the wait-for loop. No dependency on xUnit attributes.
2. **`IGrainCallCollectorProvider`** — an interface that test fixtures implement; the `WaitForAssertionAsync` extension methods operate on this interface.

### How it works

```
                  ┌──────────────────────────────────────────────────┐
                  │         GrainCallCollectionFilter                │
                  │                                                  │
  Invoke ────────►│  [Subscriber A] ──► Channel<TriggerEvent>       │
                  │  [Subscriber B] ──► Channel<TriggerEvent>       │
                  │  [Subscriber C] ──► Channel<TriggerEvent>       │
                  │                                                  │
                  │  Each subscriber has:                            │
                  │  • Bounded channel (capacity 4096)               │
                  │  • Optional testId filter                        │
                  │  • Optional predicate filter                     │
                  │                                                  │
                  │  Recording enabled when subscriberCount > 0     │
                  └──────────────────────────────────────────────────┘

  WaitForAssertionAsync:
  1. Subscribe to filter (with grain-id predicate)
  2. Try assertion immediately (pre-flight)
  3. ReadAllAsync on channel → retry assertion on each event
  4. Unsubscribe when done (via IDisposable)
```

1. **`GrainCallCollectionFilter`** broadcasts a `GrainCallTriggerEvent` (grain id, interface name, method name, test id, timestamp) to every active subscriber after each grain call completes. Each subscriber gets its own bounded `Channel<GrainCallTriggerEvent>` so concurrent readers never compete for events.

2. **`WaitForAssertionAsync`** subscribes with a predicate filter (default: match by `GrainId`), tries the assertion immediately (fast path), then reads events from the channel and retries the assertion on each trigger until it passes or the safety-net timeout fires.

3. **`RequestContextScope`** prevents self-triggering: when the assertion itself calls the grain under test, `RequestContext["test-assertion"]` is set, and the filter skips the call.

### Scalability (1000s of parallel tests)

The design is optimized for high concurrency:

- **Zero cost when idle** — `Invoke` checks a `volatile bool` (`recordingEnabled`). When no subscribers are active, this is a single volatile read per grain call.
- **Per-subscriber channels** — no contention between concurrent `WaitForAssertionAsync` calls; each gets its own channel.
- **Immutable snapshot broadcast** — the subscriber list is copy-on-write. The filter snapshots the reference outside the lock before publishing, so subscribe/unsubscribe never blocks event emission.
- **Bounded channels with fail-loud overflow** — channels use `BoundedChannelFullMode.Wait` but the publisher uses `TryWrite` and throws when full, surfacing broken test slots immediately.
- **Predicate filtering at silo edge** — optional per-subscriber predicates run inside the silo call context, reducing channel traffic to only relevant events.

## Building blocks

### 1. GrainCallCollectionFilter

The core `IIncomingGrainCallFilter`. Broadcasts events to per-subscriber channels, manages subscriber lifecycle.

```csharp
public sealed class GrainCallCollectionFilter : IIncomingGrainCallFilter, IDisposable
{
    public const int SubscriberChannelCapacity = 4096;

    // Subscribe for trigger events. Returns IDisposable to unsubscribe.
    public IDisposable Subscribe(
        out ChannelReader<GrainCallTriggerEvent> reader,
        string? testId = null,
        Predicate<IIncomingGrainCallContext>? filter = null);

    // IIncomingGrainCallFilter.Invoke — invokes the call, then broadcasts if recording is active.
    public async Task Invoke(IIncomingGrainCallContext context);
}

// Event emitted per grain call.
public readonly record struct GrainCallTriggerEvent(
    GrainId TargetGrainId,
    string InterfaceName,
    string MethodName,
    string? TestId,
    DateTimeOffset Timestamp);
```

### 2. IGrainCallCollectorProvider

Interface that test fixtures implement. Extends `IGrainFactory` so fixtures can resolve grains directly.

```csharp
public interface IGrainCallCollectorProvider : IGrainFactory
{
    GrainCallCollectionFilter CallCollector { get; }
    TimeSpan WaitForAssertionAsyncTimeout { get; set; }
}
```

### 3. SiloFixture (sample test fixture)

A production-style fixture that implements `IGrainCallCollectorProvider`:

```csharp
public class SiloFixture : IAsyncLifetime, IGrainCallCollectorProvider
{
    public GrainCallCollectionFilter CallCollector { get; } = new();
    public TimeSpan WaitForAssertionAsyncTimeout { get; set; }
        = IGrainCallCollectorProvider.DefaultWaitForAssertionAsyncTimeout;

    public async ValueTask InitializeAsync()
    {
        var builder = new InProcessTestClusterBuilder(initialSilosCount: 1);
        builder.ConfigureSilo((options, siloBuilder) =>
        {
            // Register the filter as both a singleton and IIncomingGrainCallFilter.
            siloBuilder.Services.AddSingleton(CallCollector);
            siloBuilder.Services.AddSingleton<IIncomingGrainCallFilter>(CallCollector);

            // Your storage, streams, services...
            ConfigureSilo(options, siloBuilder);
        });

        Cluster = builder.Build();
        await Cluster.DeployAsync();
    }

    // Delegates all IGrainFactory methods to Cluster.Client
    // ...
}
```

### 4. WaitForAssertionAsync (C# 14 extension)

The test-facing API. Extension methods on `IGrainCallCollectorProvider`.

```csharp
extension(IGrainCallCollectorProvider provider)
{
    // Already-resolved grain (default trigger: match by GrainId)
    public Task WaitForAssertionAsync<TGrain>(
        TGrain grain,
        Func<TGrain, Task> assertion) where TGrain : IGrain;

    // With return value
    public Task<TOutput> WaitForAssertionAsync<TGrain, TOutput>(
        TGrain grain,
        Func<TGrain, Task<TOutput>> assertion) where TGrain : IGrain;

    // Custom trigger predicate
    public Task WaitForAssertionAsync<TGrain>(
        TGrain grain,
        Predicate<IIncomingGrainCallContext> trigger,
        Func<TGrain, Task> assertion) where TGrain : IGrain;

    // Id-types matrix (string, Guid, long keys)
    public Task WaitForAssertionAsync<TGrainInterface>(
        string primaryKey,
        Func<TGrainInterface, Task> assertion)
        where TGrainInterface : IGrainWithStringKey;
}
```

### 5. RequestContextScope

Prevents assertion calls from being recorded as grain call activity.

```csharp
using (RequestContextScope.ForAssertion())
{
    await assertion(grain);
}
```

The filter checks `RequestContext.Get("test-assertion") is true` and skips the call.

## Usage patterns

### Pattern A: IClassFixture (one cluster per test class)

```csharp
public sealed class MyTests(SiloFixture fixture) : IClassFixture<SiloFixture>
{
    [Fact]
    public async Task Grain_state_updates_after_rpc()
    {
        var grain = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        await grain.Add(5);

        await fixture.WaitForAssertionAsync(
            grain,
            static async g => Assert.Equal(5, await g.GetValue()));
    }
}
```

### Pattern B: Collection fixture (shared cluster across test classes)

```csharp
[CollectionDefinition("SharedCluster")]
public class SharedClusterCollection : ICollectionFixture<SiloFixture>;

[Collection("SharedCluster")]
public sealed class MyTests(SiloFixture fixture)
{
    [Fact]
    public async Task Stream_delivery_updates_consumer()
    {
        var producer = fixture.GetGrain<IStreamProducerGrain>(Guid.NewGuid().ToString());
        var consumer = fixture.GetGrain<IStreamConsumerGrain>(Guid.NewGuid().ToString());
        var streamId = Guid.NewGuid();

        await consumer.Subscribe(streamId);
        await producer.Publish(streamId, 42);

        await fixture.WaitForAssertionAsync(
            consumer,
            static async c => Assert.Equal(42, await c.GetLastValue()));
    }
}
```

### Custom trigger predicate

When you need to trigger on arbitrary grain calls (not just the target grain):

```csharp
await fixture.WaitForAssertionAsync(
    grain,
    trigger: _ => true, // fire on ANY grain call
    assertion: static async g =>
    {
        var value = await g.GetValue();
        Assert.Equal(42, value);
    });
```

### Value-returning assertions

```csharp
var result = await fixture.WaitForAssertionAsync<ICounterGrain, int>(
    grain,
    static async g =>
    {
        var value = await g.GetValue();
        Assert.True(value > 0);
        return value;
    });
```

### Id-types matrix overloads

```csharp
// Resolve grain by string key
await fixture.WaitForAssertionAsync<IMyGrain>("my-key",
    static async g => Assert.True(await g.IsReady()));

// Resolve grain by Guid key
await fixture.WaitForAssertionAsync<IMyGrain>(Guid.Parse("..."),
    static async g => Assert.NotNull(await g.GetState()));
```

### Domain-specific extensions

Add domain-specific overloads that accept value-object ids:

```csharp
extension(IGrainCallCollectorProvider provider)
{
    public Task WaitForAssertionAsync(
        LocationId locationId,
        Func<ILocationGrain, Task> assertion)
        => provider.WaitForAssertionAsync(
            provider.GetGrain<ILocationGrain>(locationId.Value),
            assertion);
}
```

## Prevent self-trigger loops

When the assertion itself calls the grain under test, the filter would record that call and trigger another retry. `RequestContextScope.ForAssertion()` sets `RequestContext["test-assertion"] = true` for the assertion's duration, and the filter skips calls with that marker.

## Orleans-specific guidance

- **Stream deliveries** pass through the `IIncomingGrainCallFilter` on the consumer grain — no extra instrumentation needed.
- **One-way RPC calls** pass through the incoming grain call filter on the receiving silo.
- **`InProcessTestCluster`** runs silos in-process, so a single `GrainCallCollectionFilter` instance is shared.
- For parallel test safety, each test uses a **unique grain ID** and the wait loop filters by `TargetId`. Do not reuse grain IDs across tests.

## Sample

See the full working sample at `skills/dotnet/orleans/async-assertion/assets/sample/`.
