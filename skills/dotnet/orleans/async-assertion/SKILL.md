---
name: async-assertion
description: Deterministic async assertions for Orleans tests using a shared grain call log, per-test markers, and event-driven retry without polling.
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

1. **Framework-agnostic core** — the grain call filter, the shared log, the wait-for loop. No dependency on xUnit attributes.
2. **xUnit integration** — a `BeforeAfterTestAttribute` and an optional base class that automate per-test registration. This layer is thin and replaceable for other test frameworks.

### How it works

```
                    ┌─────────────────────────────────────────────┐
                    │         GrainCallCollectionFilter            │
                    │                                             │
  Invoke ──Append──►│  [pos5][pos6][pos7][pos8][pos9][pos10]     │
                    │   ▲                         ▲               │
                    │   │ marker TestA (pos5)      │ marker TestB │
                    │   │                         │  (pos9)       │
                    │                                             │
                    │  Low watermark = min(5, 9) = 5              │
                    │  → entries before pos 5 already pruned      │
                    └─────────────────────────────────────────────┘

  When TestA completes → remove marker → watermark = 9 → prune pos 5–8
```

1. **`GrainCallCollectionFilter`** records grain IDs into a shared append-only log. Each test records a marker at the current log position when it starts (`RegisterTestStart`) and removes it when it ends (`RegisterTestEnd`). Entries older than all active markers are pruned.

2. **`WaitForAssertionAsync`** reads the start position for the current test, tries the assertion immediately (fast path), then enters a scan-and-wait loop: scan the log for a matching `GrainId`, retry the assertion on match, or wait for the next append.

3. **`RequestContextScope`** prevents self-triggering: when the assertion itself calls the grain under test, `RequestContext["test-assertion"]` is set, and the filter skips the call.

### Scalability (1000s of parallel tests)

The design is optimized for high concurrency:

- **Zero cost when idle** — `Invoke` checks a `volatile int` (`_activeTestCount`). When no tests use the pattern, this is a single volatile read per grain call.
- **Shared log, not per-test channels** — no channel creation/teardown per test, no competitive message consumption.
- **Append under lock, microsecond hold time** — one `List.Add` per call. No allocations on the append path.
- **TCS-swap broadcast** — each append wakes all waiters. Each waiter does a nanosecond `GrainId ==` check.
- **Marker-based pruning** — memory bounded by `(grain calls/sec) × (longest active test duration)`. Pruning runs only on `RegisterTestEnd`.
- **Lock-free marker tracking** — `ConcurrentDictionary` for markers, `Interlocked` for the active count.

## Building blocks

### 1. GrainCallCollectionFilter

The core `IIncomingGrainCallFilter`. Records grain IDs, manages per-test markers, and broadcasts to waiters.

```csharp
public sealed class GrainCallCollectionFilter : IIncomingGrainCallFilter
{
    private readonly Lock syncLock = new();
    private readonly List<GrainId> entries = [];
    private readonly ConcurrentDictionary<string, int> testMarkers = new(StringComparer.Ordinal);
    private int baseOffset;
    private TaskCompletionSource signal = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile int activeTestCount;

    // Register a test, returns the current log position as the test's start marker.
    public int RegisterTestStart(string testId);

    // Unregister a test, prune entries older than all active markers.
    public void RegisterTestEnd(string testId);

    // Scan from startPosition for targetGrainId. Returns true on first match.
    public bool HasMatchSince(int startPosition, GrainId targetGrainId, out int newPosition);

    // Wait until the next append, for use with CancellationToken.
    public Task WaitForNewEntryAsync(CancellationToken cancellationToken);

    // IIncomingGrainCallFilter.Invoke — invokes the call, then appends if recording is active.
    public async Task Invoke(IIncomingGrainCallContext context);
}
```

The filter is framework-agnostic. Call `RegisterTestStart`/`RegisterTestEnd` from any test lifecycle hook.

### 2. AsyncClusterFixture

Wraps the test cluster and the filter. Provides `RegisterForCurrentTest()` which stores the fixture in `TestContext.Current.KeyValueStorage` for attribute discovery.

```csharp
public class AsyncClusterFixture : IAsyncLifetime
{
    public GrainCallCollectionFilter CallCollector { get; }
    public IClusterClient GrainFactory { get; }
    public IStreamProvider StreamProvider { get; }

    // Delegates to CallCollector.RegisterTestStart.
    public int RegisterTestStart(string testId);

    // Delegates to CallCollector.RegisterTestEnd.
    public void RegisterTestEnd(string testId);

    // Store this fixture in TestContext.Current.KeyValueStorage for the current test.
    public void RegisterForCurrentTest();
}
```

### 3. WaitForAssertionAsync (C# 14 extension)

The test-facing API. Reads the marker position from `TestContext.KeyValueStorage`, tries the assertion immediately, then enters the scan-and-wait loop.

```csharp
extension(AsyncClusterFixture fixture)
{
    public async Task WaitForAssertionAsync<T>(
        T grain,
        Func<T, Task> assertion,
        CancellationToken cancellationToken)
        where T : IGrain
    {
        var startPosition = GetStartPosition();
        var collector = fixture.CallCollector;
        var targetGrainId = grain.GetGrainId();

        // Try immediately (fast path).
        Exception? assertionException = await TryAssertAsync(grain, assertion);
        if (assertionException is null) return;

        var scanPosition = startPosition;
        while (true)
        {
            if (collector.HasMatchSince(scanPosition, targetGrainId, out var newPosition))
            {
                scanPosition = newPosition;
                assertionException = await TryAssertAsync(grain, assertion);
                if (assertionException is null) return;
            }
            else
            {
                scanPosition = newPosition;
                await collector.WaitForNewEntryAsync(cts.Token);
            }
        }
    }
}
```

### 4. CollectGrainCallsAttribute (xUnit integration)

A `BeforeAfterTestAttribute` that bridges the xUnit test lifecycle to the filter. In `Before`, it calls `RegisterTestStart`; in `After`, `RegisterTestEnd`.

```csharp
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false, Inherited = true)]
public sealed class CollectGrainCallsAttribute : BeforeAfterTestAttribute
{
    internal const string FixtureKey = nameof(AsyncClusterFixture);
    internal const string StartPositionKey = "GrainCallLog.StartPosition";

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current?.KeyValueStorage.TryGetValue(FixtureKey, out var obj) == true
            && obj is AsyncClusterFixture fixture)
        {
            var position = fixture.RegisterTestStart(test.UniqueID);
            TestContext.Current.KeyValueStorage[StartPositionKey] = position;
        }
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current?.KeyValueStorage.TryGetValue(FixtureKey, out var obj) == true
            && obj is AsyncClusterFixture fixture)
        {
            fixture.RegisterTestEnd(test.UniqueID);
        }
    }
}
```

xUnit discovers `IBeforeAfterTestAttribute` on assembly, collection definition, test class, and test method — **not** on fixture types. That's why the attribute goes on the test class.

### 5. AsyncAssertionTestBase (xUnit integration)

Optional base class that applies `[CollectGrainCalls]` (inherited by derived classes) and calls `RegisterForCurrentTest()` in the constructor.

```csharp
[CollectGrainCalls]
public abstract class AsyncAssertionTestBase
{
    protected AsyncClusterFixture Fixture { get; }

    protected AsyncAssertionTestBase(AsyncClusterFixture fixture)
    {
        Fixture = fixture;
        fixture.RegisterForCurrentTest();
    }
}
```

### 6. RequestContextScope

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
public sealed class MyTests(AsyncClusterFixture fixture)
    : AsyncAssertionTestBase(fixture), IClassFixture<AsyncClusterFixture>
{
    [Fact]
    public async Task Grain_state_updates_after_rpc()
    {
        var grain = Fixture.GrainFactory.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        await grain.Add(5);

        await Fixture.WaitForAssertionAsync(
            grain,
            static async g => Assert.Equal(5, await g.GetValue()),
            cancellationToken: TestContext.Current.CancellationToken);
    }
}
```

### Pattern B: Collection fixture (shared cluster across test classes)

```csharp
[CollectionDefinition("AsyncCluster")]
public class AsyncClusterCollection : ICollectionFixture<AsyncClusterFixture>;

[Collection("AsyncCluster")]
public sealed class MyTests(AsyncClusterFixture fixture)
    : AsyncAssertionTestBase(fixture)
{
    [Fact]
    public async Task Stream_delivery_updates_consumer()
    {
        var producer = Fixture.GrainFactory.GetGrain<IStreamProducerGrain>(Guid.NewGuid().ToString());
        var consumer = Fixture.GrainFactory.GetGrain<IStreamConsumerGrain>(Guid.NewGuid().ToString());
        var streamId = Guid.NewGuid();

        await consumer.Subscribe(streamId);
        await producer.Publish(streamId, 42);

        await Fixture.WaitForAssertionAsync(
            consumer,
            static async c => Assert.Equal(42, await c.GetLastValue()),
            cancellationToken: TestContext.Current.CancellationToken);
    }
}
```

### Manual wiring (without base class)

If you prefer not to use the base class, apply the attribute directly and call `RegisterForCurrentTest()` yourself:

```csharp
[CollectGrainCalls]
public sealed class MyTests(AsyncClusterFixture fixture) : IClassFixture<AsyncClusterFixture>
{
    public MyTests(AsyncClusterFixture fixture) : this(fixture)
    {
        fixture.RegisterForCurrentTest();
    }
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
