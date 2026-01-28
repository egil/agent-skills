using OrleansAsyncAssertion.Grains;
using Xunit;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Exercises deterministic async waits based on Orleans activities.
/// </summary>
public sealed class AsyncAssertionTests
{
    /// <summary>
    /// Validates that regular RPC calls can trigger an assertion retry.
    /// </summary>
    [Fact]
    public async Task Waits_for_regular_rpc_completion()
    {
        await using var fixture = await new ClusterFixture().InitializeAsync();
        var grain = fixture.GrainFactory.GetGrain<ICounterGrain>("rpc");

        await grain.Add(5);

        await fixture.WaitForAssertionAsync(
            grain,
            static async grain => Assert.Equal(5, await grain.GetValue()),
            cancellationToken: TestContext.Current.CancellationToken);

        await fixture.WaitForAssertionAsync<ICounterGrain>(
            async () => Assert.Equal(5, await grain.GetValue()),
            counterGrain => counterGrain.Add(0),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Validates that one-way RPC calls can trigger an assertion retry.
    /// </summary>
    [Fact]
    public async Task Waits_for_one_way_rpc_completion()
    {
        await using var fixture = await new ClusterFixture().InitializeAsync();
        var grain = fixture.GrainFactory.GetGrain<ICounterGrain>("oneway");
        await grain.AddOneWay(3);

        await fixture.WaitForAssertionAsync(
            grain,
            static async grain => Assert.Equal(3, await grain.GetValue()),
            trigger: counterGrain => counterGrain.AddOneWay(0),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Validates that stream deliveries can trigger an assertion retry.
    /// </summary>
    [Fact]
    public async Task Waits_for_stream_delivery_completion()
    {
        await using var fixture = await new ClusterFixture().InitializeAsync();
        var producer = fixture.GrainFactory.GetGrain<IStreamProducerGrain>("producer");
        var consumer = fixture.GrainFactory.GetGrain<IStreamConsumerGrain>("consumer");
        var streamId = Guid.NewGuid();

        await consumer.Subscribe(streamId);

        await producer.Publish(streamId, 42);

        await fixture.WaitForAssertionAsync(
            async () => Assert.Equal(42, await consumer.GetLastValue()),
            cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Validates that concurrent waits do not cross-talk between test IDs.
    /// </summary>
    [Fact]
    public async Task Parallel_waits_are_isolated_by_test_id()
    {
        await using var fixture = await new ClusterFixture().InitializeAsync();
        var grainA = fixture.GrainFactory.GetGrain<ICounterGrain>("parallel-a");
        var grainB = fixture.GrainFactory.GetGrain<ICounterGrain>("parallel-b");

        var callA = Task.Run(
            async () =>
            {
                await grainA.Add(1);
            },
            TestContext.Current.CancellationToken);

        var callB = Task.Run(
            async () =>
            {
                await grainB.Add(2);
            },
            TestContext.Current.CancellationToken);

        await Task.WhenAll(callA, callB);

        var waitA = fixture.WaitForAssertionAsync(
            grainA,
            static async grain => Assert.Equal(1, await grain.GetValue()),
            cancellationToken: TestContext.Current.CancellationToken);

        var waitB = fixture.WaitForAssertionAsync(
            grainB,
            static async grain => Assert.Equal(2, await grain.GetValue()),
            cancellationToken: TestContext.Current.CancellationToken);

        await Task.WhenAll(waitA, waitB);
    }
}
