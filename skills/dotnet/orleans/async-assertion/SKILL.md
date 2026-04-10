---
name: async-assertion
description: WaitForAssertion pattern using an IIncomingGrainCallFilter and Channel<T> to drive deterministic assertion retries in Orleans tests without polling.
---

# Async WaitForAssertion via Grain Call Filter

Use this skill when tests depend on asynchronous side-effects (RPCs, events, stream deliveries) and polling would be flaky. The pattern intercepts completed grain calls with an `IIncomingGrainCallFilter`, pipes them into a `Channel<T>`, and re-runs an assertion whenever a relevant call completes.

## When to use
- You need deterministic tests without `Task.Delay` loops.
- The system under test uses Orleans grain-to-grain calls or streams.
- Tests may run in parallel and must not interfere.

## Core idea
- Intercept grain calls with an `IIncomingGrainCallFilter` and record them to a channel.
- Filter only calls to the **target grain** relevant to the test.
- Re-run the assertion when a matching grain call completes.
- Suppress self-trigger when the assertion itself makes grain calls using `RequestContext`.

## Building blocks

### 1) GrainCallCollectionFilter
An `IIncomingGrainCallFilter` that records completed grain calls into a bounded channel. Assertion calls (marked via `RequestContext`) and Orleans internal calls are excluded.

```csharp
public sealed class GrainCallCollectionFilter : IIncomingGrainCallFilter, IDisposable
{
    private readonly Channel<IIncomingGrainCallContext> allActivities = Channel.CreateBounded<IIncomingGrainCallContext>(
        new BoundedChannelOptions(1_000)
        {
            SingleReader = false, SingleWriter = true, Capacity = 1_000, FullMode = BoundedChannelFullMode.DropOldest,
        });

    private bool collectionEnabled;

    public IAsyncEnumerable<IIncomingGrainCallContext> GetCalls(CancellationToken cancellationToken)
        => allActivities.Reader.ReadAllAsync(cancellationToken);

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        await context.Invoke();

        if (!collectionEnabled || IsOrleansInternalCall(context) || RequestContext.Get("test-assertion") is true)
        {
            return;
        }

        await allActivities.Writer.WriteAsync(context);
    }

    public void Dispose()
    {
        allActivities.Writer.TryComplete();
    }

    public void EnableRecording()
    {
        collectionEnabled = true;
    }

    private static bool IsOrleansInternalCall(IIncomingGrainCallContext context)
    {
        return context.InterfaceType.ToString().StartsWith("Orleans.Runtime.", StringComparison.Ordinal);
    }
}
```

### 2) WaitForAssertion extension
Listens on the grain call channel, filtering by target grain ID. Re-runs the assertion on each matching call until it passes or times out.

```csharp
public static class AsyncClusterFixtureWaitForExtensions
{
    extension(AsyncClusterFixture fixture)
    {
        public async Task WaitForAssertionAsync<T>(T grain, Func<T, Task> assertion, CancellationToken cancellationToken)
            where T : IGrain
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var calls = fixture.GetCalls(cts.Token);

            if (!Debugger.IsAttached)
            {
                cts.CancelAfter(TimeSpan.FromSeconds(5));
            }

            // Try the assertion immediately in case the side-effect already completed.
            Exception? assertionException = await TryAssertAsync(grain, assertion);
            if (assertionException is null)
            {
                return;
            }

            var targetGrainId = grain.GetGrainId();
            try
            {
                await foreach (var context in calls)
                {
                    if (context.TargetId != targetGrainId)
                    {
                        continue;
                    }

                    assertionException = await TryAssertAsync(grain, assertion);
                    if (assertionException is null)
                    {
                        return;
                    }
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                assertionException = new TimeoutException(
                    $"Timeout waiting for assertion.{Environment.NewLine}{assertionException?.Message}",
                    assertionException);
            }

            if (assertionException is not null)
            {
                throw assertionException;
            }
        }

        private static async Task<Exception?> TryAssertAsync<T>(T grain, Func<T, Task> assertion)
        {
            try
            {
                using (RequestContextScope.ForAssertion())
                {
                    await assertion(grain);
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
```

## Prevent self-trigger loops
When the assertion itself calls the grain under test, the filter would see that call and trigger another retry. To prevent this:

1. **`RequestContextScope.ForAssertion()`** sets `RequestContext["test-assertion"] = true` for the duration of the assertion.
2. **`GrainCallCollectionFilter`** skips any call where `RequestContext.Get("test-assertion") is true`.

```csharp
// Inside the WaitForAssertion loop
using (RequestContextScope.ForAssertion())
{
    await assertion(grain);
}
```

## Cluster fixture setup
Register the `GrainCallCollectionFilter` as an incoming grain call filter on each silo. Call `EnableRecording()` after the cluster is deployed to start capturing calls.

```csharp
public class AsyncClusterFixture : IAsyncLifetime
{
    private GrainCallCollectionFilter? callCollection;

    public async ValueTask InitializeAsync()
    {
        callCollection = new GrainCallCollectionFilter();
        var builder = new InProcessTestClusterBuilder();
        builder.ConfigureSilo((options, siloBuilder) =>
        {
            siloBuilder.AddMemoryGrainStorageAsDefault();
            siloBuilder.AddMemoryGrainStorage("PubSubStore");
            siloBuilder.AddMemoryStreams(StreamConstants.StreamProviderName);
            siloBuilder.AddIncomingGrainCallFilter(callCollection);
        });
        cluster = builder.Build();
        await cluster.DeployAsync();
        callCollection.EnableRecording();
    }
}
```

## Orleans-specific guidance
- Stream delivery in Orleans goes through grain call filters on the consumer grain, so stream-based tests work without extra instrumentation.
- One-way RPC calls still pass through the incoming grain call filter on the receiving silo.
- The `InProcessTestCluster` runs silos in-process, so a single `GrainCallCollectionFilter` instance registered via `AddIncomingGrainCallFilter` is shared across silos.
- For parallel test safety, each test uses a unique grain ID and the filter matches by `TargetId`.

## Trigger filters
The simplified approach filters by grain identity (`GrainId`). To scope triggers differently:
- `context.TargetId` — the grain receiving the call
- `context.InterfaceType` — the grain interface being called
- `context.InterfaceMethod` — the specific method being invoked

## Sample
See the Orleans async assertion sample:
- `skills/dotnet/orleans/async-assertion/assets/sample`
