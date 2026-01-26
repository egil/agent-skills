---
name: orleans-grainservice-cache
description: Use when implementing or validating the Orleans pattern where a per-silo GrainService caches data by subscribing to a data grain that persists observer references, so updates continue after grain deactivation or silo restarts/migration.
---

# Orleans GrainService Cache Subscription Pattern

Use this skill to implement or verify the pattern where a data grain persists references to per-silo grain services so cache updates survive grain lifecycle events.

## Problem this solves
You need an in-memory cache of data from a "data grain" that stays current when the data changes. Orleans Observers are ideal, but normal POCO services (e.g., singletons) lack an Orleans address and must resubscribe on an interval to maintain subscriptions. This creates overhead and causes stale data between resubscriptions when the data grain deactivates or its silo crashes.

GrainServices are addressable, so the data grain can persist their GrainIds and notify them directly. Subscriptions survive grain deactivation, silo crashes, silo restarts, and red/green deployments. Failed notifications (e.g., silo down) are handled gracefully — the GrainService automatically resubscribes on startup. GrainServices also unsubscribe cleanly during normal shutdowns.

## Pattern summary
- A single data grain owns the authoritative value and persists subscriber/observer IDs (GrainIds).
- Each silo hosts a `GrainService` that subscribes once on startup and writes to a POCO singleton cache (`IDataCache`).
- The data grain rehydrates subscriptions on activation and notifies observers via `ObserverManager`.
- Subscriptions survive grain deactivation, silo crashes, silo restarts, and red/green deployments.
- Any type can implement `IDataGrainObserver`, which implements `IGrainObserver`, so observers are not limited to a `GrainService`.

## Contracts
```csharp
/// <summary>
/// Authoritative data grain contract.
/// </summary>
public interface IDataGrain : IGrainWithStringKey
{
    /// <summary>
    /// Registers an observer for update notifications.
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

/// <summary>
/// Observer interface that can be implemented by grain services or regular grains.
/// </summary>
public interface IDataGrainObserver : IGrainObserver
{
    /// <summary>
    /// Called by the authoritative data grain when its value changes.
    /// </summary>
    /// <remarks>
    /// AlwaysInterleave allows notifications to be processed while the observer is subscribing.
    /// </remarks>
    [AlwaysInterleave]
    Task OnDataUpdated(string grainKey, string? value);
}
```

## Data grain (authoritative + persistent subscribers)
```csharp
/// <summary>
/// Persisted state for the data grain.
/// </summary>
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
/// Authoritative data owner that notifies observers on changes.
/// </summary>
public sealed class DataGrain([PersistentState("data")] IPersistentState<DataGrainState> state,
    ILogger<DataGrain> logger) : Grain, IDataGrain
{
    /// <summary>
    /// ObserverManager keeps in-memory references and handles fan-out.
    /// </summary>
    private readonly ObserverManager<GrainId, IDataGrainObserver> observerManager =
        new(TimeSpan.FromDays(365 * 10), logger);

    /// <summary>
    /// Rehydrate observers from persisted GrainIds after activation.
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
    /// Persist and register the observer, then send the latest value.
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
    /// Remove the observer from in-memory and persisted subscriber sets.
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
    /// Return the current value without notifying observers.
    /// </summary>
    public Task<string?> GetValue() => Task.FromResult(state.State.Value);

    /// <summary>
    /// Update the value, persist state, and notify observers.
    /// </summary>
    public async Task UpdateValue(string value)
    {
        state.State.Value = value;
        await state.WriteStateAsync();
        await NotifySubscribersAsync(value);
    }

    /// <summary>
    /// Notify all observers and remove unavailable ones from persistent state.
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
                // The hosting silo is down; remove the observer and allow it to resubscribe on restart.
                state.State.Subscribers = state.State.Subscribers.Remove(observer.GetGrainId());
                subscribersChangedDuringNotification = true;

                // Rethrow to ensure ObserverManager also removes the observer from its in-memory collection.
                throw;
            }
        });

        if (subscribersChangedDuringNotification)
        {
            await state.WriteStateAsync();
        }
    }
}
```

## POCO singleton cache (recommended)

Use a regular singleton that can be injected into any grain or POCO/non-Orleans type.

```csharp
/// <summary>
/// Small cache interface used by grains and non-Orleans consumers.
/// </summary>
public interface IDataCache
{
    /// <summary>
    /// Returns the cached value for a grain key (or loads it if missing).
    /// </summary>
    Task<string?> GetValue(string grainKey);
}

/// <summary>
/// POCO cache that the grain service writes into.
/// </summary>
public sealed class DataCache(IGrainFactory grainFactory) : IDataCache
{
    /// <summary>
    /// Tracks cached values keyed by grain id.
    /// </summary>
    private readonly ConcurrentDictionary<string, Task<string?>> cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Lazily loads missing values from the authoritative data grain.
    /// </summary>
    public Task<string?> GetValue(string grainKey)
        => cache.GetOrAdd(grainKey, async key =>
        {
            var grain = grainFactory.GetGrain<IDataGrain>(key);
            var value = await grain.GetValue();
            return value;
        });

    /// <summary>
    /// Called by the grain service to refresh the cached value.
    /// </summary>
    public void OnDataUpdated(string grainKey, string? value)
        => cache.AddOrUpdate(
            grainKey,
            _ => Task.FromResult(value),
            (_, _) => Task.FromResult(value));
}
```

## Per-silo grain service (addressable)

```csharp
/// <summary>
/// Interface necessary to register the grain service with Orleans.
/// </summary>
public interface ICacheGrainService : IDataGrainObserver, Orleans.Services.IGrainService
{
}

/// <summary>
/// Per-silo grain service that subscribes once and updates the POCO cache.
/// </summary>
public sealed partial class CacheGrainService(
    DataCache dataCache,
    GrainId grainId,
    Silo silo,
    IGrainFactory grainFactory,
    ILogger<CacheGrainService> logger)
    : GrainService(grainId, silo, NullLoggerFactory.Instance), ICacheGrainService
{
    private IDataGrainObserver? observerReference;

    /// <summary>
    /// Start the service and subscribe to the data grain.
    /// </summary>
    public override async Task Start()
    {
        await base.Start();
        await SubscribeToDataGrainAsync();
    }

    /// <summary>
    /// Stop the service and unsubscribe.
    /// </summary>
    public override async Task Stop()
    {
        if (observerReference is not null)
        {
            var grain = grainFactory.GetGrain<IDataGrain>(DataGrainConstants.GrainKey);
            await grain.Unsubscribe(observerReference);
        }

        await base.Stop();
    }

    /// <summary>
    /// Receives notifications and updates the singleton cache.
    /// </summary>
    public Task OnDataUpdated(string grainKey, string? value)
    {
        dataCache.OnDataUpdated(grainKey, value);
        LogCacheUpdatedLog(this.GetPrimaryKeyString(), grainKey, value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Subscribe this grain service to the shared data grain.
    /// </summary>
    private async Task SubscribeToDataGrainAsync()
    {
        observerReference = this.AsReference<IDataGrainObserver>();
        var grain = grainFactory.GetGrain<IDataGrain>(DataGrainConstants.GrainKey);
        await grain.Subscribe(observerReference);
        LogSubscribed(this.GetPrimaryKeyString());
    }

    [LoggerMessage(LogLevel.Information, Message = "Cache updated on {Silo} for grain {GrainKey} -> {Value}")]
    private partial void LogCacheUpdatedLog(string silo, string grainKey, string? value);

    [LoggerMessage(LogLevel.Information, Message = "Subscribed cache grain service on {Silo}")]
    private partial void LogSubscribed(string silo);
}
```

## Grain key constant

```csharp
/// <summary>
/// Stable grain key used by all services and tests.
/// </summary>
public static class DataGrainConstants
{
    /// <summary>
    /// Shared grain key for the authoritative data grain.
    /// </summary>
    public const string GrainKey = "global-cache";
}
```

## Registration

```csharp
siloBuilder.AddGrainService<CacheGrainService>();
siloBuilder.Services.AddSingleton<DataCache>();
siloBuilder.Services.AddSingleton<IDataCache>(services => services.GetRequiredService<DataCache>());
```

## Validation checklist

- **Initial subscription:** each silo cache sees the initial `null` value for the shared grain key, or whatever makes sense for the cache value type.
- **Update fan-out:** `UpdateValue` notifies all per-silo services.
- **Deactivation survival:** deactivate the data grain and verify updates still reach services without resubscription.
- **Silo crash tolerance:** stop a non-hosting silo; updates still flow to remaining services.

## Test setup (replicate for validation)
Use an in-process `TestCluster` with two silos, register the grain service, and assert that updates survive deactivation and silo loss.

### Cluster fixture
```csharp
/// <summary>
/// Builds and tears down a shared Orleans test cluster.
/// </summary>
public sealed class ClusterFixture
{
    /// <summary>
    /// Cluster instance used by tests.
    /// </summary>
    public TestCluster Cluster { get; private set; } = null!;

    /// <summary>
    /// Convenience access to the cluster grain factory.
    /// </summary>
    public IGrainFactory GrainFactory => Cluster.GrainFactory;

    /// <summary>
    /// Start a two-silo test cluster with the cache grain service configured.
    /// </summary>
    public async ValueTask<ClusterFixture> InitializeAsync()
    {
        var builder = new TestClusterBuilder();
        builder.Options.InitialSilosCount = 2;
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        Cluster = builder.Build();
        await Cluster.DeployAsync();
        return this;
    }

    /// <summary>
    /// Stop and dispose the cluster.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await Cluster.StopAllSilosAsync();
        await Cluster.DisposeAsync();
    }

    /// <summary>
    /// Resolve a registered service from all active silos.
    /// </summary>
    public IEnumerable<T> GetServiceFromActiveSilos<T>()
    {
        return Cluster
            .GetActiveSilos()
            .SelectMany(siloHandle => Cluster
                .GetSiloServiceProvider(siloHandle.SiloAddress)
                .GetService<IEnumerable<T>>() ?? Enumerable.Empty<T>());
    }

    /// <summary>
    /// Resolve which silo hosts a given grain activation.
    /// </summary>
    public async ValueTask<SiloAddress> GetHostingSiloAsync(IGrain grain)
    {
        var grainId = grain.GetGrainId();
        var managementGrain = GrainFactory.GetGrain<IManagementGrain>(0);
        var activations = await managementGrain.GetDetailedGrainStatistics();
        var grainSiloAddress = activations.FirstOrDefault(stat => stat.GrainId == grainId)?.SiloAddress;
        Assert.NotNull(grainSiloAddress);
        return grainSiloAddress;
    }

    private sealed class SiloConfigurator : ISiloConfigurator
    {
        /// <summary>
        /// Register storage, the grain service, and the POCO cache.
        /// </summary>
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.AddMemoryGrainStorageAsDefault();
            siloBuilder.AddGrainService<CacheGrainService>();
            siloBuilder.Services.AddSingleton<DataCache>();
            siloBuilder.Services.AddSingleton<IDataCache>(services => services.GetRequiredService<DataCache>());
        }
    }
}
```

### Tests

```csharp
/// <summary>
/// Validates that grain-service observers survive lifecycle events.
/// </summary>
public sealed class CacheGrainServiceSubscriptionTests
{
    [Fact]
    public async Task GrainService_subscribing_to_data_grain_and_receives_notifications()
    {
        await using var fixture = await new ClusterFixture().InitializeAsync();
        var dataGrain = fixture.GrainFactory.GetGrain<IDataGrain>(DataGrainConstants.GrainKey);
        var siloDataCaches = fixture.GetServiceFromActiveSilos<IDataCache>();

        await Assert.AllAsync(siloDataCaches, async cache =>
            Assert.Null(await cache.GetValue(DataGrainConstants.GrainKey)));

        await dataGrain.UpdateValue("v1");

        await Assert.AllAsync(siloDataCaches, async cache =>
            Assert.Equal("v1", await cache.GetValue(DataGrainConstants.GrainKey)));
    }

    [Fact]
    public async Task GrainService_subscriptions_survive_data_grain_deactivation()
    {
        await using var fixture = await new ClusterFixture().InitializeAsync();
        var dataGrain = fixture.GrainFactory.GetGrain<IDataGrain>(DataGrainConstants.GrainKey);
        var siloDataCaches = fixture.GetServiceFromActiveSilos<IDataCache>();

        await dataGrain.UpdateValue("v1");

        await dataGrain.Cast<IGrainManagementExtension>().DeactivateOnIdle();
        await dataGrain.UpdateValue("v2");

        await Assert.AllAsync(siloDataCaches, async cache =>
            Assert.Equal("v2", await cache.GetValue(DataGrainConstants.GrainKey)));
    }

    [Fact]
    public async Task Subscriptions_and_notifications_handle_silo_crashes()
    {
        await using var fixture = await new ClusterFixture().InitializeAsync();
        var dataGrain = fixture.GrainFactory.GetGrain<IDataGrain>(DataGrainConstants.GrainKey);
        var dataGrainSiloAddress = await fixture.GetHostingSiloAsync(dataGrain);
        var silo = fixture.Cluster.GetActiveSilos().First(x => !x.SiloAddress.Equals(dataGrainSiloAddress));
        await silo.StopSiloAsync(stopGracefully: false);

        await dataGrain.UpdateValue("v1");

        Assert.Equal("v1", await fixture.GrainFactory
            .GetGrain<ICacheDataDependentGrain>(DataGrainConstants.GrainKey)
            .GetValue());
    }
}
```

## Gotchas
- Use `IGrainObserver` for the observer interface so regular grains/POCOs can also subscribe.
- The grain service still needs `IGrainService` on its public interface to register with Orleans.
- Persist `GrainId` references, not direct observer references.
- `ObserverManager` removes failed observers from its in-memory collection. Catch `SiloUnavailableException` in the notification handler to also remove them from persisted state.

## References
- [Sample implementation](assets/sample)
- [Orleans Observers](https://learn.microsoft.com/en-us/dotnet/orleans/grains/observers?pivots=orleans-10-0)
- [Orleans GrainServices](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grainservices?pivots=orleans-10-0)
