using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Services;
using OrleansServiceObserver.Contracts;

namespace OrleansServiceObserver.Grains;

/// <summary>
/// Interface necessary to add a grain service to a orleans cluster.
/// </summary>
public interface ICacheGrainService : IDataGrainObserver, IGrainService
{
}

/// <summary>
/// A per-silo grain service that caches data and receives observer updates.
/// </summary>
/// <remarks>
/// Reentrant allows observer callbacks to arrive while the service awaits other work.
/// </remarks>
public sealed partial class CacheGrainService(
    DataCache dataCache,
    GrainId grainId,
    Silo silo,
    IGrainFactory grainFactory,
    ILogger<CacheGrainService> logger) : GrainService(grainId, silo, NullLoggerFactory.Instance), ICacheGrainService
{
    private IDataGrainObserver? observerReference;

    /// <summary>
    /// Starts the service and registers its observer subscription.
    /// </summary>
    public override async Task Start()
    {
        await base.Start();
        await SubscribeToDataGrainAsync();
    }

    /// <summary>
    /// Stops the service.
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
    /// Receives notifications from the data grain and updates the local cache.
    /// </summary>
    public Task OnDataUpdated(string grainKey, string? value)
    {
        dataCache.OnDataUpdated(grainKey, value);
        LogCacheUpdatedLog(this.GetPrimaryKeyString(), grainKey, value);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Subscribes this grain service to the shared data grain.
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