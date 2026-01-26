using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using OrleansServiceObserver.Grains;
using Xunit;

namespace OrleansServiceObserver.Tests;

/// <summary>
/// Builds and tears down a shared Orleans test cluster for the collection.
/// </summary>
public sealed class ClusterFixture
{
    public TestCluster Cluster { get; private set; } = null!;

    public IGrainFactory GrainFactory => Cluster.GrainFactory;

    public async ValueTask<ClusterFixture> InitializeAsync()
    {
        var builder = new TestClusterBuilder();
        builder.Options.InitialSilosCount = 2;
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        Cluster = builder.Build();
        await Cluster.DeployAsync();
        return this;
    }

    public async ValueTask DisposeAsync()
    {
        await Cluster.StopAllSilosAsync();
        await Cluster.DisposeAsync();
    }

    public IEnumerable<T> GetServiceFromActiveSilos<T>()
    {
        return Cluster
            .GetActiveSilos()
            .SelectMany(siloHandle => Cluster
                .GetSiloServiceProvider(siloHandle.SiloAddress)
                .GetService<IEnumerable<T>>() ?? Enumerable.Empty<T>());
    }

    public async ValueTask<SiloAddress> GetHostingSiloAsync(IGrain grain)
    {
        var grainId = grain.GetGrainId();
        var managementGrain = GrainFactory.GetGrain<IManagementGrain>(0);
        var activations = await managementGrain.GetDetailedGrainStatistics();
        var grainSiloAddress = activations.FirstOrDefault(stat => stat.GrainId == grainId)?.SiloAddress;
        Assert.NotNull(grainSiloAddress);
        return grainSiloAddress;
    }

    /// <summary>
    /// Configures per-silo services and storage for the test cluster.
    /// </summary>
    private sealed class SiloConfigurator : ISiloConfigurator
    {
        /// <summary>
        /// Registers storage, grain services, and tracing filters on each silo.
        /// </summary>
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.AddMemoryGrainStorageAsDefault();

            // Configuration of grain service and data cache service
            siloBuilder.AddGrainService<CacheGrainService>();

            // Register the implementation of DataCache such that CacheGrainService can take a dependency on it,
            // and it can have methods that allows updating the cache, that are not included in IDataCache.
            siloBuilder.Services.AddSingleton<DataCache>();
            siloBuilder.Services.AddSingleton<IDataCache>(services => services.GetRequiredService<DataCache>());
        }
    }
}