using Egil.Orleans.Testing;
using Orleans.Streams;
using Orleans.TestingHost;
using OrleansAsyncAssertion.Grains;
using Xunit;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Builds and tears down an Orleans test cluster with a
/// <see cref="GrainActivityCollector"/> from the <c>Egil.Orleans.Testing</c> package.
/// </summary>
/// <remarks>
/// <para>
/// Use as an <see cref="IClassFixture{TFixture}"/> for per-class clusters or as a
/// collection fixture for shared clusters. No base class or per-test attribute is required.
/// </para>
/// <para>
/// The fixture implements <see cref="IGrainActivityWaiter"/> and forwards to the collector,
/// so tests can call <c>fixture.WaitForAssertionAsync(...)</c> directly.
/// </para>
/// </remarks>
public class SiloFixture : IAsyncLifetime, IGrainActivityWaiter
{
    /// <summary>The stream provider obtained from the cluster client.</summary>
    private IStreamProvider? streamProvider;

    /// <summary>Gets the collector that observes grain calls and storage operations in the silo.</summary>
    public GrainActivityCollector Collector { get; } = new();

    /// <summary>Gets the grain factory from the cluster client.</summary>
    public IGrainFactory GrainFactory => Cluster?.Client ?? throw new InvalidOperationException("Test cluster not running.");

    /// <summary>Gets the stream provider configured for the test cluster.</summary>
    public IStreamProvider StreamProvider => streamProvider ?? throw new InvalidOperationException("Test cluster not running.");

    /// <summary>
    /// Gets the service provider from the test silo for resolving DI services.
    /// </summary>
    public IServiceProvider Services => Cluster?.GetSiloServiceProvider() ?? throw new InvalidOperationException("Test cluster not running.");

    /// <summary>Gets or sets the deployed test cluster instance.</summary>
    protected InProcessTestCluster? Cluster { get; set; }

    /// <summary>
    /// Creates and deploys the test cluster with the grain activity collector attached.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        var builder = new InProcessTestClusterBuilder(initialSilosCount: 1);

        builder.ConfigureSilo((options, siloBuilder) =>
        {
            siloBuilder.AddMemoryGrainStorageAsDefault();
            siloBuilder.AddMemoryGrainStorage("PubSubStore");
            siloBuilder.AddMemoryStreams(StreamConstants.StreamProviderName);

            // Observes incoming grain calls, and collects operations from the "Default" storage provider.
            siloBuilder.AddGrainActivityCollector(Collector)
                .CollectStorageActivityFromDefault();

            ConfigureSilo(options, siloBuilder);
        });

        builder.ConfigureClient(clientBuilder =>
        {
            clientBuilder.AddMemoryStreams(StreamConstants.StreamProviderName);
        });

        Cluster = builder.Build();
        await Cluster.DeployAsync();
        streamProvider = Cluster.Client.GetStreamProvider(StreamConstants.StreamProviderName);
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
    /// Stops all silos and disposes the cluster and collector.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Cluster is not null)
        {
            await Cluster.DisposeAsync();
        }

        Collector.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets a grain reference from the cluster client.
    /// </summary>
    /// <typeparam name="TGrainInterface">The grain interface type.</typeparam>
    /// <param name="primaryKey">The grain primary key.</param>
    /// <returns>The grain reference.</returns>
    public TGrainInterface GetGrain<TGrainInterface>(string primaryKey)
        where TGrainInterface : IGrainWithStringKey
        => GrainFactory.GetGrain<TGrainInterface>(primaryKey);

    /// <summary>
    /// Forwards the wait primitive to <see cref="Collector"/> so tests can call
    /// <c>fixture.WaitForAssertionAsync(...)</c> instead of <c>fixture.Collector.WaitForAssertionAsync(...)</c>.
    /// </summary>
    Task<TResult> IGrainActivityWaiter.WaitForAssertionAsync<TResult>(
        Func<ValueTask<TResult>> assertion,
        Predicate<GrainActivity>? filter,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
        => ((IGrainActivityWaiter)Collector).WaitForAssertionAsync(assertion, filter, timeout, cancellationToken);
}
