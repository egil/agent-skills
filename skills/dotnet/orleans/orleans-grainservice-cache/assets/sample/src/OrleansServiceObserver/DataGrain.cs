using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace OrleansServiceObserver.Grains;

/// <summary>
/// Represents the authoritative data owner.
/// </summary>
/// <remarks>
/// Persists subscriber references so notifications survive deactivation and rehydration.
/// </remarks>
public interface IDataGrain : IGrainWithStringKey
{
    /// <summary>
    /// Registers a per-silo grain service observer for update notifications.
    /// </summary>
    public Task Subscribe(IDataGrainObserver subscriber);

    /// <summary>
    /// Removes a previously registered observer.
    /// </summary>
    public Task Unsubscribe(IDataGrainObserver subscriber);

    /// <summary>
    /// Mutates the grain's state and notifies observers.
    /// </summary>
    public Task UpdateValue(string value);

    /// <summary>
    /// Reads the current value for verification/testing.
    /// </summary>
    public Task<string?> GetValue();
}

[GenerateSerializer]
public sealed class DataGrainState
{
    /// <summary>
    /// Authoritative value mirrored in per-silo caches.
    /// </summary>
    [Id(0)]
    public string? Value { get; set; }

    /// <summary>
    /// Persisted observer references so subscriptions survive deactivation.
    /// </summary>
    [Id(1)]
    public ImmutableHashSet<GrainId> Subscribers
    {
        get => field ?? [];
        set => field = value ?? [];
    }
}

/// <summary>
/// The authoritative data owner. It notifies per-silo cache grain services on changes.
/// </summary>
public sealed partial class DataGrain(
    [PersistentState("data")] IPersistentState<DataGrainState> state,
    IClusterMembershipService membership,
    ILogger<DataGrain> logger) : Grain, IDataGrain
{
    /// <summary>
    /// Persists and registers a per-silo observer, then sends the latest value.
    /// </summary>
    public async Task Subscribe(IDataGrainObserver subscriber)
    {
        var subscriberId = subscriber.GetGrainId();
        state.State.Subscribers = state.State.Subscribers.Add(subscriberId);
        await state.WriteStateAsync();
        await subscriber.OnDataUpdated(this.GetPrimaryKeyString(), state.State.Value);
    }

    /// <summary>
    /// Unregisters and removes a per-silo observer.
    /// </summary>
    public async Task Unsubscribe(IDataGrainObserver subscriber)
    {
        var subscriberId = subscriber.GetGrainId();
        var storedSubs = state.State.Subscribers.Remove(subscriberId);
        if (state.State.Subscribers != storedSubs)
        {
            state.State.Subscribers = storedSubs;
            await state.WriteStateAsync();
        }
    }

    /// <summary>
    /// Returns the current value without notifying observers.
    /// </summary>
    public Task<string?> GetValue() => Task.FromResult(state.State.Value);

    /// <summary>
    /// Updates the value, persists state, and notifies observers.
    /// </summary>
    public async Task UpdateValue(string value)
    {
        state.State.Value = value;
        await state.WriteStateAsync();
        await NotifySubscribersAsync(value);
    }

    /// <summary>
    /// Fans out to all observers concurrently, then removes only confirmed-dead silo generations.
    /// </summary>
    private async Task NotifySubscribersAsync(string? value)
    {
        var subscribers = state.State.Subscribers;
        if (subscribers.Count == 0)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var outcomes = await ObserverFanout.DeliverConcurrentlyAsync(subscribers, async subscriberId =>
        {
            var observer = GrainFactory.GetGrain<IDataGrainObserver>(subscriberId);
            await observer.OnDataUpdated(this.GetPrimaryKeyString(), value);
        });

        var membershipSnapshot = membership.CurrentSnapshot;
        var confirmedDead = ImmutableHashSet.CreateBuilder<GrainId>();
        foreach (var outcome in outcomes.Where(outcome => outcome.Error is not null))
        {
            var status = "unknown";
            var remove = false;
            if (SystemTargetGrainId.TryParse(outcome.ObserverId, out var systemTarget))
            {
                var siloAddress = systemTarget.GetSiloAddress();
                membershipSnapshot.Members.TryGetValue(siloAddress, out var member);
                (status, remove) = ObserverMembershipCleanup.GetDecision(
                    siloAddress,
                    member?.Status,
                    membershipSnapshot.Members.Keys);
            }

            LogObserverDeliveryFailed(outcome.ObserverId.ToString(), status, remove, outcome.Error!);
            if (remove)
            {
                confirmedDead.Add(outcome.ObserverId);
            }
        }

        LogFanoutCompleted(subscribers.Count, outcomes.Count(outcome => outcome.Error is not null), confirmedDead.Count, stopwatch.ElapsedMilliseconds);

        if (confirmedDead.Count > 0)
        {
            state.State.Subscribers = await ObserverMembershipCleanup.RemoveConfirmedAsync(
                state.State.Subscribers,
                confirmedDead.ToImmutable(),
                updatedObservers =>
                {
                    state.State.Subscribers = updatedObservers;
                    return state.WriteStateAsync();
                });
        }
    }

    /// <summary>
    /// Logs a failed observer call together with its membership-based cleanup decision.
    /// </summary>
    [LoggerMessage(LogLevel.Warning, Message = "Observer delivery failed for {ObserverId}; membership status {MembershipStatus}; cleanup {Removed}")]
    private partial void LogObserverDeliveryFailed(string observerId, string membershipStatus, bool removed, Exception error);

    /// <summary>
    /// Logs the aggregate result and duration of one observer fanout.
    /// </summary>
    [LoggerMessage(LogLevel.Information, Message = "Observer fanout completed for {Attempted} observers; {Failed} failed; {Removed} removed; duration {DurationMs} ms")]
    private partial void LogFanoutCompleted(int attempted, int failed, int removed, long durationMs);
}