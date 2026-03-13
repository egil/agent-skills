using System.Diagnostics;

namespace OrleansAsyncAssertion.Tests.Simplified;

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

            Exception? assertionException = null;
            var targetGrainId = grain.GetGrainId();
            try
            {
                await foreach (var context in calls)
                {
                    if (context.TargetId != targetGrainId)
                    {
                        continue;
                    }

                    try
                    {
                        using (RequestContextScope.ForAssertion())
                        {
                            await assertion(grain);
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        assertionException = ex;
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
    }
}