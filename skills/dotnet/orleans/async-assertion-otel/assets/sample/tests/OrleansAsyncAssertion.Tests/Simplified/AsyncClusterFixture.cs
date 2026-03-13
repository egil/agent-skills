using Orleans.Streams;
using Orleans.TestingHost;
using OrleansAsyncAssertion.Grains;
using Xunit;

namespace OrleansAsyncAssertion.Tests.Simplified;

public class AsyncClusterFixture : IAsyncLifetime
{
    private InProcessTestCluster? cluster;
    private GrainCallCollectionFilter? callCollection;

    public InProcessTestCluster Cluster => cluster ?? throw new InvalidOperationException($"Call {nameof(InitializeAsync)} first before accessing the test cluster.");

    public IClusterClient GrainFactory => Cluster.Client;

    public IStreamProvider StreamProvider => Cluster.Client.GetStreamProvider(StreamConstants.StreamProviderName);

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

    public IAsyncEnumerable<IIncomingGrainCallContext> GetCalls(CancellationToken cancellationToken)
        => callCollection?.GetCalls(cancellationToken)
           ?? throw new InvalidOperationException($"Call {nameof(InitializeAsync)} first before accessing the test cluster.");
}