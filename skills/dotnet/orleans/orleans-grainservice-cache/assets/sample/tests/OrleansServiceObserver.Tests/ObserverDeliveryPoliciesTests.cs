using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using OrleansServiceObserver.Grains;
using Xunit;

namespace OrleansServiceObserver.Tests;

/// <summary>
/// Verifies observer fanout and membership-aware cleanup policies.
/// </summary>
public sealed class ObserverDeliveryPoliciesTests
{
    /// <summary>
    /// Verifies live deliveries finish within one timeout while many failed deliveries run concurrently.
    /// </summary>
    [Fact]
    public async Task Concurrent_fanout_delivers_live_observers_within_one_timeout_window()
    {
        var observerIds = Enumerable.Range(0, 84)
            .Select(index => GrainId.Create("cache", index.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            .ToArray();
        var liveObservers = observerIds.Take(4).ToHashSet();
        var timeoutWindow = TimeSpan.FromSeconds(1);
        var allDeliveriesStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var liveDeliveriesCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedCount = 0;
        var liveCompletedCount = 0;
        var stopwatch = Stopwatch.StartNew();

        var fanout = ObserverFanout.DeliverConcurrentlyAsync(observerIds, async observerId =>
        {
            if (Interlocked.Increment(ref startedCount) == observerIds.Length)
            {
                allDeliveriesStarted.SetResult();
            }

            await allDeliveriesStarted.Task.WaitAsync(timeoutWindow, TestContext.Current.CancellationToken);
            if (liveObservers.Contains(observerId))
            {
                if (Interlocked.Increment(ref liveCompletedCount) == liveObservers.Count)
                {
                    liveDeliveriesCompleted.SetResult();
                }

                return;
            }

            await Task.Delay(timeoutWindow, TestContext.Current.CancellationToken);
            throw new TimeoutException("Simulated unreachable observer.");
        });

        await liveDeliveriesCompleted.Task.WaitAsync(timeoutWindow, TestContext.Current.CancellationToken);
        var results = await fanout.WaitAsync(timeoutWindow + timeoutWindow, TestContext.Current.CancellationToken);

        Assert.Equal(observerIds.Length, results.Length);
        Assert.Equal(liveObservers.Count, results.Count(result => result.Error is null));
        Assert.Equal(observerIds.Length - liveObservers.Count, results.Count(result => result.Error is TimeoutException));
        Assert.True(stopwatch.Elapsed < timeoutWindow + timeoutWindow);
    }

    /// <summary>
    /// Verifies membership statuses which prove a generation cannot recover permit cleanup.
    /// </summary>
    [Theory]
    [InlineData(SiloStatus.Dead)]
    [InlineData(SiloStatus.Stopping)]
    [InlineData(SiloStatus.ShuttingDown)]
    public void Confirmed_terminal_status_allows_cleanup(SiloStatus status)
    {
        var observerSilo = SiloAddress.New(IPAddress.Loopback, 11111, 10);

        Assert.True(ObserverMembershipCleanup.GetDecision(observerSilo, status, [observerSilo]).Remove);
    }

    /// <summary>
    /// Verifies membership statuses which might recover retain failed observer IDs.
    /// </summary>
    [Theory]
    [InlineData(SiloStatus.Active)]
    [InlineData(SiloStatus.Joining)]
    [InlineData(SiloStatus.Created)]
    public void Transitional_or_active_status_retains_observer(SiloStatus status)
    {
        var observerSilo = SiloAddress.New(IPAddress.Loopback, 11111, 10);

        Assert.False(ObserverMembershipCleanup.GetDecision(observerSilo, status, [observerSilo]).Remove);
    }

    /// <summary>
    /// Verifies missing membership is not treated as proof of death.
    /// </summary>
    [Fact]
    public void Unknown_membership_retains_observer()
    {
        var observerSilo = SiloAddress.New(IPAddress.Loopback, 11111, 10);

        Assert.Equal(("unknown", false), ObserverMembershipCleanup.GetDecision(observerSilo, null, []));
    }

    /// <summary>
    /// Verifies a newer process generation at the same endpoint supersedes the failed generation.
    /// </summary>
    [Fact]
    public void Newer_generation_at_same_endpoint_allows_cleanup()
    {
        var observerSilo = SiloAddress.New(IPAddress.Loopback, 11111, 10);
        var newerGeneration = SiloAddress.New(IPAddress.Loopback, 11111, 11);

        Assert.Equal(
            ("superseded-generation", true),
            ObserverMembershipCleanup.GetDecision(observerSilo, SiloStatus.Active, [newerGeneration]));
    }

    /// <summary>
    /// Verifies multiple confirmed removals invoke persistence once for the whole fanout.
    /// </summary>
    [Fact]
    public async Task Confirmed_observer_removals_are_persisted_in_one_write()
    {
        var currentObservers = Enumerable.Range(0, 6)
            .Select(index => GrainId.Create("cache", index.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            .ToImmutableHashSet();
        var confirmedDead = currentObservers.Take(4).ToImmutableHashSet();
        var writeCount = 0;
        ImmutableHashSet<GrainId>? persistedObservers = null;

        var result = await ObserverMembershipCleanup.RemoveConfirmedAsync(
            currentObservers,
            confirmedDead,
            updatedObservers =>
            {
                writeCount++;
                persistedObservers = updatedObservers;
                return Task.CompletedTask;
            });

        Assert.Equal(1, writeCount);
        Assert.Equal(currentObservers.Except(confirmedDead), persistedObservers);
        Assert.Equal(persistedObservers, result);
    }
}