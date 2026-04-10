using System.Diagnostics;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Provides a WaitForAssertion extension method for <see cref="AsyncClusterFixture"/>.
/// </summary>
public static class AsyncClusterFixtureWaitForExtensions
{
    extension(AsyncClusterFixture fixture)
    {
        /// <summary>
        /// Waits for an assertion to pass by re-running it whenever a grain call targeting the
        /// specified grain completes. Times out after 5 seconds unless a debugger is attached.
        /// </summary>
        /// <typeparam name="T">The grain type.</typeparam>
        /// <param name="grain">The grain whose calls trigger assertion retries.</param>
        /// <param name="assertion">The assertion to execute.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task that completes when the assertion passes.</returns>
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

        /// <summary>
        /// Executes the assertion and returns any exception thrown, or <c>null</c> if it passed.
        /// </summary>
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