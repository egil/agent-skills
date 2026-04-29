using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streams;
using Orleans.TestingHost;
using OrleansAsyncAssertion.Grains;
using Xunit;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Builds and tears down an Orleans test cluster with grain call collection enabled.
/// Implements <see cref="IGrainCallCollectorProvider"/> so that the
/// <c>WaitForAssertionAsync</c> extension methods work directly on this fixture.
/// </summary>
/// <remarks>
/// <para>
/// Use as an <see cref="IClassFixture{TFixture}"/> for per-class clusters or as a
/// collection fixture for shared clusters. No base class or per-test attribute is required —
/// the <see cref="GrainCallCollectionFilter"/> uses per-subscriber channels, so each
/// <c>WaitForAssertionAsync</c> call subscribes and unsubscribes automatically.
/// </para>
/// <para>
/// The filter is registered both as a singleton (for DI access) and as an
/// <see cref="IIncomingGrainCallFilter"/> in the silo pipeline.
/// </para>
/// </remarks>
public class SiloFixture : IAsyncLifetime, IGrainCallCollectorProvider
{
    /// <summary>The underlying test cluster, created during <see cref="InitializeAsync"/>.</summary>
    private InProcessTestCluster? cluster;

    /// <summary>The grain factory obtained from the cluster client.</summary>
    private IGrainFactory? grainFactory;

    /// <summary>The stream provider obtained from the cluster client.</summary>
    private IStreamProvider? streamProvider;

    /// <summary>The grain call collection filter, created before cluster deployment.</summary>
    public GrainCallCollectionFilter CallCollector { get; } = new();

    /// <inheritdoc />
    public TimeSpan WaitForAssertionAsyncTimeout { get; set; } = IGrainCallCollectorProvider.DefaultWaitForAssertionAsyncTimeout;

    /// <summary>
    /// Gets the service provider from the test silo for resolving DI services.
    /// </summary>
    public IServiceProvider Services => Cluster?.GetSiloServiceProvider() ?? throw new InvalidOperationException("Test cluster not running.");

    /// <summary>Gets the deployed test cluster instance.</summary>
    protected InProcessTestCluster? Cluster { get => cluster; set => cluster = value; }

    /// <summary>
    /// Creates and deploys the test cluster with a grain call collection filter.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        var builder = new InProcessTestClusterBuilder(initialSilosCount: 1);

        builder.ConfigureSilo((options, siloBuilder) =>
        {
            siloBuilder.AddMemoryGrainStorageAsDefault();
            siloBuilder.AddMemoryGrainStorage("PubSubStore");
            siloBuilder.AddMemoryStreams(StreamConstants.StreamProviderName);

            siloBuilder.Services.AddSingleton(CallCollector);
            siloBuilder.Services.AddSingleton<IIncomingGrainCallFilter>(CallCollector);

            ConfigureSilo(options, siloBuilder);
        });

        builder.ConfigureClient(clientBuilder =>
        {
            clientBuilder.AddMemoryStreams(StreamConstants.StreamProviderName);
        });

        cluster = builder.Build();
        await cluster.DeployAsync();
        grainFactory = cluster.Client;
        streamProvider = cluster.Client.GetStreamProvider(StreamConstants.StreamProviderName);
    }

    /// <summary>
    /// Override to add additional silo configuration (storage providers, services, etc.).
    /// </summary>
    /// <param name="options">The per-silo options.</param>
    /// <param name="siloBuilder">The silo builder.</param>
    protected virtual void ConfigureSilo(InProcessTestSiloSpecificOptions options, ISiloBuilder siloBuilder)
    {
    }

    /// <summary>
    /// Stops all silos and disposes the cluster and call collector.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (cluster is not null)
        {
            await cluster.StopAllSilosAsync();
        }

        CallCollector.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets the stream provider configured for the test cluster.</summary>
    public IStreamProvider StreamProvider => streamProvider ?? throw new InvalidOperationException("Test cluster not running.");

    // ---- IGrainFactory delegation ----

    /// <inheritdoc />
    [StackTraceHidden]
    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithGuidKey
        => GrainFactory.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);

    /// <inheritdoc />
    [StackTraceHidden]
    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithIntegerKey
        => GrainFactory.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);

    /// <inheritdoc />
    [StackTraceHidden]
    public TGrainInterface GetGrain<TGrainInterface>(string primaryKey, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithStringKey
        => GrainFactory.GetGrain<TGrainInterface>(primaryKey, grainClassNamePrefix);

    /// <inheritdoc />
    public TGrainInterface GetGrain<TGrainInterface>(Guid primaryKey, string keyExtension, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithGuidCompoundKey
        => GrainFactory.GetGrain<TGrainInterface>(primaryKey, keyExtension, grainClassNamePrefix);

    /// <inheritdoc />
    public TGrainInterface GetGrain<TGrainInterface>(long primaryKey, string keyExtension, string? grainClassNamePrefix = null) where TGrainInterface : IGrainWithIntegerCompoundKey
        => GrainFactory.GetGrain<TGrainInterface>(primaryKey, keyExtension, grainClassNamePrefix);

    /// <inheritdoc />
    public TGrainObserverInterface CreateObjectReference<TGrainObserverInterface>(IGrainObserver obj) where TGrainObserverInterface : IGrainObserver
        => GrainFactory.CreateObjectReference<TGrainObserverInterface>(obj);

    /// <inheritdoc />
    public void DeleteObjectReference<TGrainObserverInterface>(IGrainObserver obj) where TGrainObserverInterface : IGrainObserver
        => GrainFactory.DeleteObjectReference<TGrainObserverInterface>(obj);

    /// <inheritdoc />
    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey)
        => GrainFactory.GetGrain(grainInterfaceType, grainPrimaryKey);

    /// <inheritdoc />
    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey)
        => GrainFactory.GetGrain(grainInterfaceType, grainPrimaryKey);

    /// <inheritdoc />
    public IGrain GetGrain(Type grainInterfaceType, string grainPrimaryKey)
        => GrainFactory.GetGrain(grainInterfaceType, grainPrimaryKey);

    /// <inheritdoc />
    public IGrain GetGrain(Type grainInterfaceType, Guid grainPrimaryKey, string keyExtension)
        => GrainFactory.GetGrain(grainInterfaceType, grainPrimaryKey, keyExtension);

    /// <inheritdoc />
    public IGrain GetGrain(Type grainInterfaceType, long grainPrimaryKey, string keyExtension)
        => GrainFactory.GetGrain(grainInterfaceType, grainPrimaryKey, keyExtension);

    /// <inheritdoc />
    public TGrainInterface GetGrain<TGrainInterface>(GrainId grainId) where TGrainInterface : IAddressable
        => GrainFactory.GetGrain<TGrainInterface>(grainId);

    /// <inheritdoc />
    public IAddressable GetGrain(GrainId grainId)
        => GrainFactory.GetGrain(grainId);

    /// <inheritdoc />
    public IAddressable GetGrain(GrainId grainId, GrainInterfaceType interfaceType)
        => GrainFactory.GetGrain(grainId, interfaceType);

    /// <inheritdoc />
    public IAddressable GetGrain(Type interfaceType, IdSpan grainKey, string grainClassNamePrefix)
        => GrainFactory.GetGrain(interfaceType, grainKey, grainClassNamePrefix);

    /// <inheritdoc />
    public IAddressable GetGrain(Type interfaceType, IdSpan grainKey)
        => GrainFactory.GetGrain(interfaceType, grainKey);

    /// <summary>
    /// Gets the cluster client grain factory. Throws if the cluster is not running.
    /// </summary>
    private IGrainFactory GrainFactory => grainFactory ?? throw new InvalidOperationException("Test cluster not running.");
}