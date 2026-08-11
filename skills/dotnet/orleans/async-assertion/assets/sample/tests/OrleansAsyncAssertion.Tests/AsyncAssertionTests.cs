using Egil.Orleans.Testing;
using OrleansAsyncAssertion.Grains;
using Xunit;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Exercises deterministic async waits based on the <c>Egil.Orleans.Testing</c> package.
/// Demonstrates using <see cref="SiloFixture"/> as an <see cref="IClassFixture{TFixture}"/>.
/// </summary>
public sealed class AsyncAssertionTests(SiloFixture fixture) : IClassFixture<SiloFixture>
{
    /// <summary>
    /// Validates that regular RPC calls can trigger an assertion retry.
    /// </summary>
    [Fact]
    public async Task Waits_for_regular_rpc_completion()
    {
        var grain = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        await grain.Add(5);

        await fixture.WaitForAssertionAsync(
            grain,
            static async grain => Assert.Equal(5, await grain.GetValue()),
            ct: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Validates that one-way RPC calls can trigger an assertion retry.
    /// </summary>
    [Fact]
    public async Task Waits_for_one_way_rpc_completion()
    {
        var grain = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        await grain.AddOneWay(3);

        await fixture.WaitForAssertionAsync(
            grain,
            static async grain => Assert.Equal(3, await grain.GetValue()),
            ct: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Validates that stream deliveries can trigger an assertion retry.
    /// </summary>
    [Fact]
    public async Task Waits_for_stream_delivery_completion()
    {
        var producer = fixture.GetGrain<IStreamProducerGrain>(Guid.NewGuid().ToString());
        var consumer = fixture.GetGrain<IStreamConsumerGrain>(Guid.NewGuid().ToString());
        var streamId = Guid.NewGuid();

        await consumer.Subscribe(streamId);
        await producer.Publish(streamId, 42);

        await fixture.WaitForAssertionAsync(
            consumer,
            static async consumer => Assert.Equal(42, await consumer.GetLastValue()),
            ct: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Validates that concurrent waits do not cross-talk between tests.
    /// Each grain has a unique ID, so the grain-scoped filter isolates them.
    /// </summary>
    [Fact]
    public async Task Parallel_waits_are_isolated_by_grain_id()
    {
        var grainA = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());
        var grainB = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        _ = grainA.Add(1);
        _ = grainB.Add(2);

        await fixture.WaitForAssertionAsync(
            grainA,
            static async grain => Assert.Equal(1, await grain.GetValue()),
            ct: TestContext.Current.CancellationToken);

        await fixture.WaitForAssertionAsync(
            grainB,
            static async grain => Assert.Equal(2, await grain.GetValue()),
            ct: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Validates that the assertion can return a value.
    /// </summary>
    [Fact]
    public async Task Value_returning_overload_works()
    {
        var grain = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        await grain.Add(7);

        var result = await fixture.WaitForAssertionAsync(
            grain,
            static async grain =>
            {
                var value = await grain.GetValue();
                Assert.Equal(7, value);
                return value;
            },
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(7, result);
    }

    /// <summary>
    /// Validates that the overload without a grain scope retries on any observed activity.
    /// </summary>
    [Fact]
    public async Task Overload_without_grain_retries_on_any_activity()
    {
        var grain = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());
        var unrelated = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        await grain.Add(9);

        var ct = TestContext.Current.CancellationToken;
        var pingTask = Task.Run(
            async () =>
            {
                await Task.Delay(50, ct);
                await unrelated.Add(1);
            },
            ct);

        await fixture.WaitForAssertionAsync(
            async () => Assert.Equal(9, await grain.GetValue()),
            ct: TestContext.Current.CancellationToken);

        await pingTask;
    }

    /// <summary>
    /// Validates that an assertion that never succeeds fails with a
    /// <see cref="WaitForAssertionTimeoutException"/> that carries the last assertion failure.
    /// </summary>
    [Fact]
    public async Task Timeout_surfaces_last_assertion_exception()
    {
        var grain = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        var exception = await Assert.ThrowsAsync<WaitForAssertionTimeoutException>(() =>
            fixture.WaitForAssertionAsync(
                grain,
                static async grain => Assert.Equal(999, await grain.GetValue()),
                timeout: TimeSpan.FromMilliseconds(500),
                ct: TestContext.Current.CancellationToken));

        Assert.NotNull(exception.InnerException);
    }

    /// <summary>
    /// Validates that the grain call feed can be observed directly when a test needs
    /// individual activity events instead of a retried assertion.
    /// </summary>
    [Fact]
    public async Task Grain_call_feed_observes_calls_for_grain()
    {
        var grain = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        await grain.Add(1);

        var call = await fixture.Collector
            .GetGrainCallsAsync(grain, includeExisting: true, TestContext.Current.CancellationToken)
            .FirstAsync(TestContext.Current.CancellationToken);

        Assert.Equal(nameof(ICounterGrain.Add), call.InterfaceMethod?.Name);
    }
}
