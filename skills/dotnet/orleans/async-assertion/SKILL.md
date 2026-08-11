---
name: async-assertion
description: Deterministic async assertions for Orleans tests using the Egil.Orleans.Testing package, which retries assertions on observed grain call and storage activity instead of polling.
---

# Async WaitForAssertion with Egil.Orleans.Testing

Use this skill when an Orleans test depends on asynchronous side-effects — RPCs, one-way calls, grain timers, reminders, or stream deliveries — and must not use polling (`Task.Delay` loops) or implementation-detail coupling (specific method names, call counts, etc.).

Do not hand-roll a grain call filter, channel plumbing, or retry loop for this. Use the [`Egil.Orleans.Testing`](https://www.nuget.org/packages/Egil.Orleans.Testing) package, which provides a `GrainActivityCollector` that observes grain calls and storage operations inside the silo, plus `WaitForAssertionAsync` overloads that retry an assertion whenever matching activity is observed.

## Design philosophy: the assertion IS the contract

Tests should express **what** must eventually become true, not **how** the system makes it true. The assertion itself is the contract — the test should never wait for a particular internal RPC sequence.

When you couple tests to implementation details you get:

- **Fragile tests** — refactoring internal call chains breaks tests even though the observable behavior is unchanged.
- **False confidence** — tests assert the wrong thing (a method was called) instead of the right thing (state changed).

`WaitForAssertionAsync` treats _any_ observed activity (optionally scoped to a single grain) as a retry trigger. It does not care which method was called or how many calls it took. The assertion alone decides pass/fail.

## When to use

- Tests depend on eventually-consistent state caused by grain-to-grain calls, timers, reminders, or stream deliveries.
- You need deterministic wait behavior (no `Task.Delay` or configurable poll intervals).
- Tests run in parallel and must not interfere with each other.

## Getting started

### 1. Install the package

```shell
dotnet add package Egil.Orleans.Testing
```

### 2. Attach the collector to the test silo

```csharp
var collector = new GrainActivityCollector();
var builder = new InProcessTestClusterBuilder(initialSilosCount: 1);

builder.ConfigureSilo((_, siloBuilder) =>
{
    siloBuilder.AddMemoryGrainStorageAsDefault();

    // Observes incoming grain calls; the builder adds storage observation.
    siloBuilder.AddGrainActivityCollector(collector)
        .CollectStorageActivityFromDefault();
});

await using var cluster = builder.Build();
await cluster.DeployAsync();
```

`AddGrainActivityCollector` registers the collector and its incoming grain call filter. The returned builder enables optional storage observation:

- `CollectStorageActivityFromDefault()` — observe the `Default` storage provider.
- `CollectStorageActivityFrom("MyProvider")` — observe a named provider.
- `CollectStorageActivity()` — observe every keyed `IGrainStorage` registered **before** the call.

### 3. Wait for the assertion

```csharp
var grain = cluster.Client.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

await grain.AddOneWay(5);

await collector.WaitForAssertionAsync(
    grain,
    static async g => Assert.Equal(5, await g.GetValue()));
```

## Test fixture integration

Implement `IGrainActivityWaiter` on the test fixture and forward to the collector, so tests can call `fixture.WaitForAssertionAsync(...)` directly. See `assets/sample/tests/OrleansAsyncAssertion.Tests/SiloFixture.cs` for a complete fixture.

```csharp
public class SiloFixture : IAsyncLifetime, IGrainActivityWaiter
{
    public GrainActivityCollector Collector { get; } = new();

    Task<TResult> IGrainActivityWaiter.WaitForAssertionAsync<TResult>(
        Func<ValueTask<TResult>> assertion,
        Predicate<GrainActivity>? filter,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
        => ((IGrainActivityWaiter)Collector).WaitForAssertionAsync(assertion, filter, timeout, cancellationToken);
}
```

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
            static async g => Assert.Equal(5, await g.GetValue()),
            ct: TestContext.Current.CancellationToken);
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
            static async c => Assert.Equal(42, await c.GetLastValue()),
            ct: TestContext.Current.CancellationToken);
    }
}
```

## API reference

`WaitForAssertionAsync` is available on any `IGrainActivityWaiter` (the collector itself, or a fixture that forwards to it). All overloads accept an optional `timeout` and `ct`.

```csharp
// Retry on activity from a single grain (preferred default).
await waiter.WaitForAssertionAsync(grain, async g => Assert.Equal(5, await g.GetValue()));
await waiter.WaitForAssertionAsync(grain, async () => Assert.Equal(5, await grain.GetValue()));

// Retry on any observed activity, useful when several grains contribute.
await waiter.WaitForAssertionAsync(async () => Assert.Equal(5, await grain.GetValue()));

// Value-returning assertions.
var value = await waiter.WaitForAssertionAsync(grain, async g => await g.GetValue());
```

Lower-level observation, when a test genuinely needs individual events (couples the test to
call flow or persistence details — prefer `WaitForAssertionAsync`):

```csharp
// Live IAsyncEnumerable feeds; use includeExisting: true to replay recent history.
await foreach (var activity in collector.GetGrainActivityAsync(cancellationToken: ct)) { }
await foreach (var call in collector.GetGrainCallsAsync(grain, includeExisting: true, ct)) { }
await foreach (var op in collector.GetStorageOperationsAsync(grain, cancellationToken: ct)) { }
```

Other package types:

- `GrainActivity` — the observed signal (`GrainId`, `Kind`, `Timestamp`, `StorageOperation`, `GrainCallContext`).
- `WaitForAssertionTimeoutException` — thrown when the timeout expires; the last assertion failure is the inner exception.
- `RequestContextScope.ForAssertion()` — marks calls made by an assertion so they do not trigger another retry (applied automatically by `WaitForAssertionAsync`).
- `ReminderTestClock` — deterministic time control for reminder-driven tests.

## Timeouts

- Default is 5 seconds, overridable per call via the `timeout` parameter.
- Process-wide default: `IGrainActivityWaiter.DefaultWaitTimeout` or the `WAIT_FOR_ASSERTION_TIMEOUT_SECONDS` environment variable.
- The timeout is bypassed automatically when a debugger is attached.

## Orleans-specific guidance

- **Stream deliveries** pass through the incoming grain call filter on the consumer grain — no extra instrumentation needed.
- **One-way RPC calls** pass through the incoming grain call filter on the receiving silo.
- **Grain timers and reminders** are observed through the storage writes and grain calls they cause; enable storage collection when the follow-up work only writes state.
- **`InProcessTestCluster`** runs silos in-process, so a single `GrainActivityCollector` instance is shared.
- For parallel test safety, each test uses a **unique grain ID** and scopes the wait to that grain. Do not reuse grain IDs across tests.

## Sample

See the full working sample at `skills/dotnet/orleans/async-assertion/assets/sample/`.

## Further reading

- Package README: <https://github.com/egil/framework/tree/main/Egil.Orleans.Testing>
- Recipes: [assertion patterns](https://github.com/egil/framework/blob/main/Egil.Orleans.Testing/docs/recipes/assertion-patterns.md), [advanced assertions](https://github.com/egil/framework/blob/main/Egil.Orleans.Testing/docs/recipes/advanced-assertions.md), [timers and reminders](https://github.com/egil/framework/blob/main/Egil.Orleans.Testing/docs/recipes/timers-and-reminders.md), [streams](https://github.com/egil/framework/blob/main/Egil.Orleans.Testing/docs/recipes/streams.md)
