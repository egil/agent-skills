using Orleans.Streams;
using Orleans.TestingHost;
using OrleansAsyncAssertion.Grains;
using Xunit;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Builds and tears down an Orleans test cluster with grain call collection enabled.
/// Exposes <see cref="RegisterTestStart"/> and <see cref="RegisterTestEnd"/> for
/// per-test marker tracking, and <see cref="RegisterForCurrentTest"/> to store
/// this fixture in <see cref="TestContext.Current"/>.<see cref="TestContext.KeyValueStorage"/>
/// for discovery by <see cref="CollectGrainCallsAttribute"/>.
/// </summary>
public class AsyncClusterFixture : IAsyncLifetime
{
    private InProcessTestCluster? cluster;
    private GrainCallCollectionFilter? callCollector;

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
    /// Gets the grain call collection filter that records grain calls.
    /// </summary>
    public GrainCallCollectionFilter CallCollector => callCollector ?? throw new InvalidOperationException($"Call {nameof(InitializeAsync)} first before accessing the call collector.");

    /// <summary>
    /// Creates and deploys the test cluster with a grain call collection filter.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        callCollector = new GrainCallCollectionFilter();

        var builder = new InProcessTestClusterBuilder();
        builder.ConfigureSilo((options, siloBuilder) =>
        {
            siloBuilder.AddMemoryGrainStorageAsDefault();
            siloBuilder.AddMemoryGrainStorage("PubSubStore");
            siloBuilder.AddMemoryStreams(StreamConstants.StreamProviderName);
            siloBuilder.AddIncomingGrainCallFilter(callCollector);
        });

        cluster = builder.Build();
        await cluster.DeployAsync();
    }

    /// <summary>
    /// Stops all silos and disposes the cluster.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (cluster is not null)
        {
            await cluster.StopAllSilosAsync();
            await cluster.DisposeAsync();
        }

        cluster = null;
        callCollector = null;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Registers a test start with the grain call collector, recording the current log position.
    /// </summary>
    /// <param name="testId">A unique identifier for the test.</param>
    /// <returns>The logical log position at the time of registration.</returns>
    public int RegisterTestStart(string testId)
        => CallCollector.RegisterTestStart(testId);

    /// <summary>
    /// Unregisters a completed test and triggers log pruning.
    /// </summary>
    /// <param name="testId">The unique identifier used in the matching <see cref="RegisterTestStart"/> call.</param>
    public void RegisterTestEnd(string testId)
        => CallCollector.RegisterTestEnd(testId);

    /// <summary>
    /// Stores this fixture in <see cref="TestContext.Current"/>.<see cref="TestContext.KeyValueStorage"/>
    /// so that <see cref="CollectGrainCallsAttribute"/> can discover it during test lifecycle hooks.
    /// Call this from the test class constructor.
    /// </summary>
    public void RegisterForCurrentTest()
    {
        TestContext.Current!.KeyValueStorage[CollectGrainCallsAttribute.FixtureKey] = this;
    }
}