using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Orleans.Utilities;

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
    Task Subscribe(IDataGrainObserver subscriber);

    /// <summary>
    /// Removes a previously registered observer.
    /// </summary>
    Task Unsubscribe(IDataGrainObserver subscriber);

    /// <summary>
    /// Mutates the grain's state and notifies observers.
    /// </summary>
    Task UpdateValue(string value);

    /// <summary>
    /// Reads the current value for verification/testing.
    /// </summary>
    Task<string?> GetValue();
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
public sealed class DataGrain([PersistentState("data")] IPersistentState<DataGrainState> state, ILogger<DataGrain> logger) : Grain, IDataGrain
{
    private readonly ObserverManager<GrainId, IDataGrainObserver> observerManager = new(TimeSpan.FromDays(365 * 10), logger);

    /// <summary>
    /// Rehydrates observer references after activation.
    /// </summary>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        foreach (var subscriberId in state.State.Subscribers)
        {
            var observer = GrainFactory.GetGrain<IDataGrainObserver>(subscriberId);
            observerManager.Subscribe(subscriberId, observer);
        }

        await base.OnActivateAsync(cancellationToken);
    }

    /// <summary>
    /// Persists and registers a per-silo observer, then sends the latest value.
    /// </summary>
    public async Task Subscribe(IDataGrainObserver subscriber)
    {
        var subscriberId = subscriber.GetGrainId();
        observerManager.Subscribe(subscriberId, subscriber);
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
        observerManager.Unsubscribe(subscriberId);
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
    /// Notifies all live observers of the latest value.
    /// </summary>
    private async Task NotifySubscribersAsync(string? value)
    {
        if (observerManager.Count == 0)
        {
            return;
        }

        var subscribersChangedDuringNotification = false;

        await observerManager.Notify(async observer =>
        {
            try
            {
                await observer.OnDataUpdated(this.GetPrimaryKeyString(), value);
            }
            catch (SiloUnavailableException)
            {
                // SiloUnavailableException indicates the silo hosting the grain service is down,
                // and there is no reason to keep the observer registered, as it will
                // resubscribe itself when it restarts.
                state.State.Subscribers = state.State.Subscribers.Remove(observer.GetGrainId());
                subscribersChangedDuringNotification = true;

                // rethrowing here ensures the observer is removed from the manager.
                throw;
            }
        });

        if (subscribersChangedDuringNotification)
        {
            await state.WriteStateAsync();
        }
    }
}