---
name: async-assertion-otel
description: WaitForAssertion pattern using OpenTelemetry Activities or ActivityListener streams; use when tests must wait for async side-effects without polling.
---

# Async WaitForAssertion via OpenTelemetry

Use this skill when tests depend on asynchronous side-effects (RPCs, events, background processing) and polling would be flaky. The pattern re-runs an assertion whenever a relevant span completes.

## When to use
- You need deterministic tests without `Task.Delay` loops.
- The system emits OpenTelemetry spans (`Activity`) you can filter.
- Tests may run in parallel and must not interfere.

## Core idea
- Capture spans into an async stream (`IAsyncEnumerable<Activity>`).
- Filter only the **trigger spans** relevant to the test.
- Re-run the assertion when a trigger span completes.
- Suppress self-trigger when the assertion itself makes RPCs.
- Support replay so waits can start *after* the trigger already happened.
 - Correlate spans with a per-test identifier so tests can run in parallel safely.

## Building blocks

### 1) Activity stream with replay
A collector that buffers recent spans and exposes a replayable async stream.

```csharp
public readonly record struct ActivityCursor(long Sequence);

public sealed class ActivityStream : IDisposable
{
    private readonly Channel<ActivityEnvelope> _channel = Channel.CreateUnbounded<ActivityEnvelope>();
    private readonly List<ActivityEnvelope> _buffer = new();
    private long _seq;

    public ActivityCursor GetCursor() => new(Interlocked.Read(ref _seq) + 1);

    public async IAsyncEnumerable<Activity> Read(
        ActivityCursor cursor,
        [EnumeratorCancellation] CancellationToken ct)
    {
        List<ActivityEnvelope> snapshot;
        lock (_buffer)
        {
            snapshot = _buffer.Where(e => e.Sequence >= cursor.Sequence).ToList();
        }

        foreach (var e in snapshot)
        {
            yield return e.Activity;
        }

        var minSeq = snapshot.Count > 0 ? snapshot[^1].Sequence + 1 : cursor.Sequence;
        await foreach (var e in _channel.Reader.ReadAllAsync(ct))
        {
            if (e.Sequence >= minSeq)
            {
                yield return e.Activity;
            }
        }
    }

    public void OnActivityStopped(Activity activity)
    {
        var seq = Interlocked.Increment(ref _seq);
        var env = new ActivityEnvelope(seq, activity);
        lock (_buffer)
        {
            _buffer.Add(env);
            if (_buffer.Count > 1024) _buffer.RemoveRange(0, _buffer.Count - 1024);
        }
        _channel.Writer.TryWrite(env);
    }

    private sealed record ActivityEnvelope(long Sequence, Activity Activity);
}
```

### 2) WaitForAssertion helper
Runs the assertion immediately, then re-runs on each trigger span until it passes or times out.

```csharp
public static async Task WaitForAssertionAsync(
    ActivityStream stream,
    ActivityCursor cursor,
    Func<Activity, bool> isTrigger,
    Func<Task> assertion,
    TimeSpan timeout,
    CancellationToken ct)
{
    var lastFailure = await TryAssertAsync(assertion);
    if (lastFailure is null) return;

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeoutCts.CancelAfter(timeout);

    await foreach (var activity in stream.Read(cursor, timeoutCts.Token))
    {
        if (!isTrigger(activity)) continue;

        lastFailure = await TryAssertAsync(assertion);
        if (lastFailure is null) return;
    }

    throw new TimeoutException("Assertion did not pass before timeout.", lastFailure);
}
```

## Prevent self-trigger loops (Option 3a)
When the assertion itself performs the same RPC you are waiting for, tag spans made during the assertion and ignore them.

**Pattern:**
- Add a lightweight filter (or interceptor) that tags spans when a `test.assertion` marker is present.
- Wrap the assertion in a scope that sets this marker.
- Ignore spans with `test.assertion=true`.

Example for Orleans using `RequestContext` + incoming grain call filter:
```csharp
// Filter adds test.assertion tag when RequestContext has "test-assertion".
if (RequestContext.Get("test-assertion") is true)
{
    activity.SetTag("test.assertion", true);
}

// Assertion scope in test
using var _ = RequestContextScope.With("test-assertion", true);
await assertion();
```

## Orleans-specific guidance
- Incoming grain call filters are enough to tag Activities; you do not need full OpenTelemetry collection in production code to use this pattern in tests.
- For one-way RPCs, the incoming filter still tags the activity on the receiving silo, so the trigger spans remain deterministic.
- Stream delivery does not always emit Orleans RPC spans; emit a lightweight Activity in the consumer grain (`ActivitySource.StartActivity`) and tag it with `stream.id`, `stream.namespace`, and `test.id`.
- To avoid explicit `RequestContext` usage in tests, tag grain calls with a filter that includes `orleans.grain_id` and use the grain reference to scope triggers.

## Trigger filters
Use stable span tags to make tests parallel-safe:
- `rpc.method` (or framework-specific tag)
- `rpc.service` or grain interface
- `rpc.orleans.target_id` (grain key)
- `test.id` (optional) for extra correlation
- `stream.id` and `stream.namespace` for stream deliveries

## Sample adapter (Orleans)
See the Orleans async assertion sample:
- `skills/dotnet/orleans/async-assertion-otel/assets/sample`
