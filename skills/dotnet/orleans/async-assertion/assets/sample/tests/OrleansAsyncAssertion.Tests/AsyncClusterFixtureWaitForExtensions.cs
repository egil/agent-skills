using System.Diagnostics;
using Xunit;

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
        /// specified grain completes. Tries the assertion immediately first. Times out after
        /// 5 seconds unless a debugger is attached.
        /// </summary>
        /// <typeparam name="T">The grain type.</typeparam>
        /// <param name="grain">The grain whose calls trigger assertion retries.</param>
        /// <param name="assertion">The assertion to execute.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task that completes when the assertion passes.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no start position marker is found, indicating the test class is missing
        /// the <see cref="CollectGrainCallsAttribute"/> or did not call
        /// <see cref="AsyncClusterFixture.RegisterForCurrentTest"/>.
        /// </exception>
        public async Task WaitForAssertionAsync<T>(T grain, Func<T, Task> assertion, CancellationToken cancellationToken)
            where T : IGrain
        {
            var startPosition = GetStartPosition();
            var collector = fixture.CallCollector;
            var targetGrainId = grain.GetGrainId();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (!Debugger.IsAttached)
            {
                cts.CancelAfter(TimeSpan.FromSeconds(5));
            }

            // Try the assertion immediately — it may already hold.
            Exception? assertionException = await TryAssertAsync(grain, assertion);
            if (assertionException is null)
            {
                return;
            }

            var scanPosition = startPosition;
            try
            {
                while (true)
                {
                    if (collector.HasMatchSince(scanPosition, targetGrainId, out var newPosition))
                    {
                        scanPosition = newPosition;
                        assertionException = await TryAssertAsync(grain, assertion);
                        if (assertionException is null)
                        {
                            return;
                        }
                    }
                    else
                    {
                        scanPosition = newPosition;
                        await collector.WaitForNewEntryAsync(cts.Token);
                    }
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Timeout waiting for assertion.{Environment.NewLine}{assertionException?.Message}",
                    assertionException);
            }
        }

        /// <summary>
        /// Retrieves the grain call log start position for the current test from
        /// <see cref="TestContext.Current"/>.<see cref="TestContext.KeyValueStorage"/>.
        /// </summary>
        /// <returns>The logical start position recorded by <see cref="CollectGrainCallsAttribute"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the start position is not found.</exception>
        private static int GetStartPosition()
        {
            if (TestContext.Current?.KeyValueStorage.TryGetValue(CollectGrainCallsAttribute.StartPositionKey, out var obj) == true
                && obj is int position)
            {
                return position;
            }

            throw new InvalidOperationException(
                $"No grain call log start position found. Apply [{nameof(CollectGrainCallsAttribute)}] to the test class " +
                $"and call {nameof(AsyncClusterFixture)}.{nameof(AsyncClusterFixture.RegisterForCurrentTest)}() in the constructor, " +
                $"or use {nameof(AsyncAssertionTestBase)} as a base class.");
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