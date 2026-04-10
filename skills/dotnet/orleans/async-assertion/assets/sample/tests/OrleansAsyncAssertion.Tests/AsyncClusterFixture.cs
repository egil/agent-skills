using Orleans.Streams;
using Orleans.TestingHost;
using OrleansAsyncAssertion.Grains;
using Xunit;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Builds and tears down an Orleans test cluster with grain call collection enabled.
/// </summary>
public class AsyncClusterFixture : IAsyncLifetime
{
    private InProcessTestCluster? cluster;
    private GrainCallCollectionFilter? callCollection;

    /// <summary>
    /// Gets the deployed test cluster instance.
    /// </summary>
    public InProcessTestCluster Cluster => cluster ?? throw new InvalidOperationException($"Call {nameof(InitializeAsync)} first before accessing the test cluster.");

    /// <summary>
    /// Gets the cluster client used to create grain references.
    /// </summary>
    public IClusterClient GrainFactory => Cluster.Client;

    /// <summary>
    /// Gets the stream provider configured for the test cluster.
    /// </summary>
    public IStreamProvider StreamProvider => Cluster.Client.GetStreamProvider(StreamConstants.StreamProviderName);

    /// <summary>
    /// Creates and deploys the test cluster with a grain call collection filter.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        callCollection = new GrainCallCollectionFilter();

        var builder = new InProcessTestClusterBuilder();
        builder.ConfigureSilo((options, siloBuilder) =>
        {
            siloBuilder.AddMemoryGrainStorageAsDefault();
            siloBuilder.AddMemoryGrainStorage("PubSubStore");
            siloBuilder.AddMemoryStreams(StreamConstants.StreamProviderName);
            siloBuilder.AddIncomingGrainCallFilter(callCollection);
        });

        cluster = builder.Build();
        await cluster.DeployAsync();
        callCollection.EnableRecording();
    }

    /// <summary>
    /// Stops all silos and disposes the cluster.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        callCollection?.Dispose();

        if (cluster is not null)
        {
            await cluster.StopAllSilosAsync();
            await cluster.DisposeAsync();
        }

        cluster = null;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns an async stream of grain call contexts observed by the collection filter.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An async enumerable of grain call contexts.</returns>
    public IAsyncEnumerable<IIncomingGrainCallContext> GetCalls(CancellationToken cancellationToken)
        => callCollection?.GetCalls(cancellationToken)
           ?? throw new InvalidOperationException($"Call {nameof(InitializeAsync)} first before accessing the test cluster.");
}