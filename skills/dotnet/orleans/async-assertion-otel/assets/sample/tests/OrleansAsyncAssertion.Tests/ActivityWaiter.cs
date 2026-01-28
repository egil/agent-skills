using System.Diagnostics;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Waits for a test assertion to pass by replaying activities that match a filter.
/// </summary>
public static class ActivityWaiter
{
    /// <summary>
    /// Re-runs the assertion whenever a matching activity completes.
    /// </summary>
    /// <param name="collector">The activity collector to read from.</param>
    /// <param name="cursor">The cursor marking the starting point for replay.</param>
    /// <param name="isTrigger">A predicate that selects relevant activities.</param>
    /// <param name="assertion">The assertion to execute.</param>
    /// <param name="timeout">The timeout for the wait operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    public static async Task WaitForAssertionAsync(
        TestTraceCollector collector,
        ActivityCursor cursor,
        Func<Activity, bool> isTrigger,
        Func<Task> assertion,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var lastFailure = await TryAssertAsync(assertion).ConfigureAwait(false);
        if (lastFailure is null)
        {
            return;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        await foreach (var activity in collector.GetActivities(cursor, timeoutCts.Token).ConfigureAwait(false))
        {
            if (!isTrigger(activity))
            {
                continue;
            }

            lastFailure = await TryAssertAsync(assertion).ConfigureAwait(false);
            if (lastFailure is null)
            {
                return;
            }
        }

        throw new TimeoutException("Assertion did not pass before timeout.", lastFailure);
    }

    /// <summary>
    /// Executes the assertion and returns any exception thrown.
    /// </summary>
    /// <param name="assertion">The assertion to execute.</param>
    /// <returns>The thrown exception, or <c>null</c> if the assertion succeeded.</returns>
    private static async Task<Exception?> TryAssertAsync(Func<Task> assertion)
    {
        try
        {
            await assertion().ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
