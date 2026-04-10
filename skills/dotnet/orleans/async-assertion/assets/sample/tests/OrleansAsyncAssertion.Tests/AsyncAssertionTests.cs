using OrleansAsyncAssertion.Grains;
using Xunit;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Exercises deterministic async waits based on Orleans grain call collection.
/// </summary>
public sealed class AsyncAssertionTests(AsyncClusterFixture fixture)
    : AsyncAssertionTestBase(fixture), IClassFixture<AsyncClusterFixture>
{
    /// <summary>
    /// Validates that regular RPC calls can trigger an assertion retry.
    /// </summary>
    [Fact]
    public async Task Waits_for_regular_rpc_completion()
    {
        var grain = Fixture.GrainFactory.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        await grain.Add(5);

        await Fixture.WaitForAssertionAsync(
            grain,
            static async grain => Assert.Equal(5, await grain.GetValue()),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Validates that one-way RPC calls can trigger an assertion retry.
    /// </summary>
    [Fact]
    public async Task Waits_for_one_way_rpc_completion()
    {
        var grain = Fixture.GrainFactory.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        await grain.AddOneWay(3);

        await Fixture.WaitForAssertionAsync(
            grain,
            static async grain => Assert.Equal(3, await grain.GetValue()),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Validates that stream deliveries can trigger an assertion retry.
    /// </summary>
    [Fact]
    public async Task Waits_for_stream_delivery_completion()
    {
        var producer = Fixture.GrainFactory.GetGrain<IStreamProducerGrain>(Guid.NewGuid().ToString());
        var consumer = Fixture.GrainFactory.GetGrain<IStreamConsumerGrain>(Guid.NewGuid().ToString());
        var streamId = Guid.NewGuid();

        await consumer.Subscribe(streamId);
        await producer.Publish(streamId, 42);

        await Fixture.WaitForAssertionAsync(
            consumer,
            static async consumer => Assert.Equal(42, await consumer.GetLastValue()),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Validates that concurrent waits do not cross-talk between test IDs.
    /// </summary>
    [Fact]
    public async Task Parallel_waits_are_isolated_by_test_id()
    {
        var grainA = Fixture.GrainFactory.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());
        var grainB = Fixture.GrainFactory.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        _ = grainA.Add(1);
        _ = grainB.Add(2);

        await Fixture.WaitForAssertionAsync(
            grainA,
            static async grain => Assert.Equal(1, await grain.GetValue()),
            cancellationToken: TestContext.Current.CancellationToken);

        await Fixture.WaitForAssertionAsync(
            grainB,
            static async grain => Assert.Equal(2, await grain.GetValue()),
            cancellationToken: TestContext.Current.CancellationToken);
    }
}