using System.Collections.Concurrent;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// An incoming grain call filter that records completed grain calls into a shared append-only log
/// with per-test marker tracking and automatic pruning. Only records calls while at least one test
/// is registered via <see cref="RegisterTestStart"/>. Assertion calls and Orleans internal calls
/// are excluded.
/// </summary>
public sealed class GrainCallCollectionFilter : IIncomingGrainCallFilter
{
    /// <summary>
    /// Guards <see cref="entries"/>, <see cref="baseOffset"/>, and <see cref="signal"/>.
    /// </summary>
    private readonly Lock syncLock = new();

    /// <summary>
    /// Append-only list of grain IDs observed by the filter.
    /// </summary>
    private readonly List<GrainId> entries = [];

    /// <summary>
    /// Maps test IDs to their start position in the log. Used for pruning and to prevent
    /// removal of entries still needed by active tests.
    /// </summary>
    private readonly ConcurrentDictionary<string, int> testMarkers = new(StringComparer.Ordinal);

    /// <summary>
    /// The logical position of <c>entries[0]</c>. Increases as entries are pruned.
    /// </summary>
    private int baseOffset;

    /// <summary>
    /// A <see cref="TaskCompletionSource"/> that is completed and swapped on each append,
    /// waking all waiters blocked in <see cref="WaitForNewEntryAsync"/>.
    /// </summary>
    private TaskCompletionSource signal = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// The number of currently active tests. Checked on the hot path to skip recording
    /// when no tests are running.
    /// </summary>
    private volatile int activeTestCount;

    /// <summary>
    /// Registers the start of a test, recording the current log position as a marker.
    /// Call this before the test body runs so that all grain calls during the test
    /// appear at positions &gt;= the returned value.
    /// </summary>
    /// <param name="testId">A unique identifier for the test (e.g., <c>IXunitTest.UniqueID</c>).</param>
    /// <returns>The logical log position at the time of registration.</returns>
    public int RegisterTestStart(string testId)
    {
        int position;
        lock (syncLock)
        {
            position = baseOffset + entries.Count;
        }

        testMarkers[testId] = position;
        Interlocked.Increment(ref activeTestCount);
        return position;
    }

    /// <summary>
    /// Unregisters a completed test and prunes log entries that are no longer needed
    /// by any active test.
    /// </summary>
    /// <param name="testId">The unique identifier used in the matching <see cref="RegisterTestStart"/> call.</param>
    public void RegisterTestEnd(string testId)
    {
        if (testMarkers.TryRemove(testId, out _))
        {
            Interlocked.Decrement(ref activeTestCount);
        }

        Prune();
    }

    /// <summary>
    /// Scans the log from <paramref name="startPosition"/> for an entry matching
    /// <paramref name="targetGrainId"/>. Returns <c>true</c> on the first match.
    /// </summary>
    /// <param name="startPosition">The logical position to start scanning from.</param>
    /// <param name="targetGrainId">The grain ID to match.</param>
    /// <param name="newPosition">
    /// Set to one past the matched entry on success, or the current end of the log on failure.
    /// Use this value as <paramref name="startPosition"/> on subsequent calls.
    /// </param>
    /// <returns><c>true</c> if a matching entry was found; otherwise <c>false</c>.</returns>
    public bool HasMatchSince(int startPosition, GrainId targetGrainId, out int newPosition)
    {
        lock (syncLock)
        {
            var startIndex = startPosition - baseOffset;
            if (startIndex < 0)
            {
                startIndex = 0;
            }

            for (var i = startIndex; i < entries.Count; i++)
            {
                if (entries[i] == targetGrainId)
                {
                    newPosition = baseOffset + i + 1;
                    return true;
                }
            }

            newPosition = baseOffset + entries.Count;
            return false;
        }
    }

    /// <summary>
    /// Returns a task that completes when a new entry is appended to the log.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes on the next append.</returns>
    public Task WaitForNewEntryAsync(CancellationToken cancellationToken)
    {
        Task task;
        lock (syncLock)
        {
            task = signal.Task;
        }

        return task.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Invokes the grain call and records the target grain ID in the log unless the call is
    /// an Orleans internal call, an assertion call, or no tests are currently active.
    /// </summary>
    /// <param name="context">The incoming grain call context.</param>
    /// <returns>A task that completes when the call and optional recording are finished.</returns>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        await context.Invoke();

        if (activeTestCount == 0
            || IsOrleansInternalCall(context)
            || RequestContext.Get("test-assertion") is true)
        {
            return;
        }

        Append(context.TargetId);
    }

    /// <summary>
    /// Appends a grain ID to the log and signals all waiters.
    /// </summary>
    /// <param name="grainId">The grain ID to record.</param>
    private void Append(GrainId grainId)
    {
        TaskCompletionSource previous;
        lock (syncLock)
        {
            entries.Add(grainId);
            previous = signal;
            signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        previous.TrySetResult();
    }

    /// <summary>
    /// Removes log entries that are older than the earliest active test marker.
    /// </summary>
    private void Prune()
    {
        var minPosition = int.MaxValue;
        foreach (var kvp in testMarkers)
        {
            if (kvp.Value < minPosition)
            {
                minPosition = kvp.Value;
            }
        }

        lock (syncLock)
        {
            // If no markers remain, prune everything.
            if (minPosition == int.MaxValue)
            {
                minPosition = baseOffset + entries.Count;
            }

            var removeCount = minPosition - baseOffset;
            if (removeCount > 0 && removeCount <= entries.Count)
            {
                entries.RemoveRange(0, removeCount);
                baseOffset += removeCount;
            }
        }
    }

    /// <summary>
    /// Determines whether the grain call is an Orleans internal call that should be ignored.
    /// </summary>
    /// <param name="context">The incoming grain call context.</param>
    /// <returns><c>true</c> if the call is internal; otherwise <c>false</c>.</returns>
    private static bool IsOrleansInternalCall(IIncomingGrainCallContext context)
    {
        return context.InterfaceType.ToString().StartsWith("Orleans.Runtime.", StringComparison.Ordinal);
    }
}