using System.Collections.Immutable;

namespace OrleansServiceObserver.Grains;

/// <summary>
/// Captures the result of one observer delivery attempt.
/// </summary>
public sealed record ObserverDeliveryResult(GrainId ObserverId, Exception? Error);

/// <summary>
/// Runs a group of observer deliveries concurrently and captures each failure independently.
/// </summary>
public static class ObserverFanout
{
    /// <summary>
    /// Starts one delivery for every observer before awaiting the group.
    /// </summary>
    public static async Task<ImmutableArray<ObserverDeliveryResult>> DeliverConcurrentlyAsync(
        IEnumerable<GrainId> observerIds,
        Func<GrainId, Task> deliver)
    {
        var results = await Task.WhenAll(observerIds.Select(async observerId =>
        {
            try
            {
                await deliver(observerId);
                return new ObserverDeliveryResult(observerId, null);
            }
            catch (Exception exception)
            {
                return new ObserverDeliveryResult(observerId, exception);
            }
        }));
        return results.ToImmutableArray();
    }
}

/// <summary>
/// Applies fail-closed cleanup rules to failed observer deliveries.
/// </summary>
public static class ObserverMembershipCleanup
{
    /// <summary>
    /// Returns the membership status and cleanup decision for a failed observer generation.
    /// </summary>
    public static (string Status, bool Remove) GetDecision(
        SiloAddress observerSilo,
        SiloStatus? exactGenerationStatus,
        IEnumerable<SiloAddress> knownSilos)
    {
        var superseded = knownSilos.Any(candidate =>
            candidate.Endpoint.Equals(observerSilo.Endpoint) && candidate.Generation > observerSilo.Generation);
        if (superseded)
        {
            return ("superseded-generation", true);
        }

        var status = exactGenerationStatus?.ToString() ?? "unknown";
        var remove = exactGenerationStatus is SiloStatus.Dead or SiloStatus.Stopping or SiloStatus.ShuttingDown;
        return (status, remove);
    }

    /// <summary>
    /// Removes all confirmed observer IDs and persists the resulting set at most once.
    /// </summary>
    public static async Task<ImmutableHashSet<GrainId>> RemoveConfirmedAsync(
        ImmutableHashSet<GrainId> currentObservers,
        ImmutableHashSet<GrainId> confirmedDead,
        Func<ImmutableHashSet<GrainId>, Task> persist)
    {
        var updatedObservers = currentObservers.Except(confirmedDead);
        if (updatedObservers.Count != currentObservers.Count)
        {
            await persist(updatedObservers);
        }

        return updatedObservers;
    }
}