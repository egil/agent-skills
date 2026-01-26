using Orleans.Core.Internal;
using OrleansServiceObserver.Contracts;
using OrleansServiceObserver.Grains;
using Xunit;

namespace OrleansServiceObserver.Tests;

/// <summary>
/// Exercises the grain-service observer pattern to ensure updates survive grain lifecycle events.
/// </summary>
public sealed class CacheGrainServiceSubscriptionTests
{
    [Fact]
    public async Task GrainService_subscribing_to_data_grain_and_receives_notifications()
    {
        await using var fixture = await new ClusterFixture().InitializeAsync();
        var dataGrain = fixture.GrainFactory.GetGrain<IDataGrain>(DataGrainConstants.GrainKey);
        var siloDataCaches = fixture.GetServiceFromActiveSilos<IDataCache>();
        await Assert.AllAsync(siloDataCaches, async cache => Assert.Null(await cache.GetValue(DataGrainConstants.GrainKey)));

        // Act - updating value in data grain
        await dataGrain.UpdateValue(value: "v1");

        // Assert
        await Assert.AllAsync(siloDataCaches, async cache => Assert.Equal("v1", await cache.GetValue(DataGrainConstants.GrainKey)));
        Assert.Equal("v1", await fixture.GrainFactory.GetGrain<ICacheDataDependentGrain>(DataGrainConstants.GrainKey).GetValue());
    }

    [Fact]
    public async Task GrainService_subscriptions_survive_data_grain_deactivation()
    {
        await using var fixture = await new ClusterFixture().InitializeAsync();
        var dataGrain = fixture.GrainFactory.GetGrain<IDataGrain>(DataGrainConstants.GrainKey);
        var siloDataCaches = fixture.GetServiceFromActiveSilos<IDataCache>();
        await dataGrain.UpdateValue("v1");

        // Act - deactivate and activate the data grain
        await dataGrain.Cast<IGrainManagementExtension>().DeactivateOnIdle();
        await dataGrain.UpdateValue("v2");

        // Assert
        await Assert.AllAsync(siloDataCaches, async cache => Assert.Equal("v2", await cache.GetValue(DataGrainConstants.GrainKey)));
        Assert.Equal("v2", await fixture.GrainFactory.GetGrain<ICacheDataDependentGrain>(DataGrainConstants.GrainKey).GetValue());
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

        Assert.Equal("v1", await fixture.GrainFactory.GetGrain<ICacheDataDependentGrain>(DataGrainConstants.GrainKey).GetValue());
    }
}