using OrleansAsyncAssertion.Grains;
using Xunit;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Exercises deterministic async waits based on Orleans grain call collection.
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
            static async grain => Assert.Equal(5, await grain.GetValue()));
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
            static async grain => Assert.Equal(3, await grain.GetValue()));
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
            static async consumer => Assert.Equal(42, await consumer.GetLastValue()));
    }

    /// <summary>
    /// Validates that concurrent waits do not cross-talk between tests.
    /// Each grain has a unique ID, so the default trigger isolates them.
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
            static async grain => Assert.Equal(1, await grain.GetValue()));

        await fixture.WaitForAssertionAsync(
            grainB,
            static async grain => Assert.Equal(2, await grain.GetValue()));
    }

    /// <summary>
    /// Validates that the assertion can return a value.
    /// </summary>
    [Fact]
    public async Task Value_returning_overload_works()
    {
        var grain = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        await grain.Add(7);

        var result = await fixture.WaitForAssertionAsync<ICounterGrain, int>(
            grain,
            static async grain =>
            {
                var value = await grain.GetValue();
                Assert.Equal(7, value);
                return value;
            });

        Assert.Equal(7, result);
    }

    /// <summary>
    /// Validates that the id-types overload resolves the grain by string key.
    /// </summary>
    [Fact]
    public async Task Id_overload_resolves_grain_by_string_key()
    {
        var key = Guid.NewGuid().ToString();
        var grain = fixture.GetGrain<ICounterGrain>(key);

        await grain.Add(11);

        await fixture.WaitForAssertionAsync<ICounterGrain>(key,
            static async grain => Assert.Equal(11, await grain.GetValue()));
    }

    /// <summary>
    /// Validates that a custom trigger predicate can widen the trigger scope.
    /// </summary>
    [Fact]
    public async Task Custom_trigger_widens_scope()
    {
        var grain = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());
        var unrelated = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

        await grain.Add(9);

        // Use a widened trigger that fires on ANY grain call, not just the target.
        var pingTask = Task.Run(async () =>
        {
            await Task.Delay(50, TestContext.Current.CancellationToken);
            await unrelated.Add(1);
        }, TestContext.Current.CancellationToken);

        await fixture.WaitForAssertionAsync(
            grain,
            trigger: _ => true,
            assertion: static async grain => Assert.Equal(9, await grain.GetValue()));

        await pingTask;
    }

    /// <summary>
    /// Validates that when the safety-net timeout fires, the last assertion
    /// exception is surfaced (not a cancellation exception).
    /// </summary>
    [Fact]
    public async Task Exhaustion_surfaces_last_assertion_exception()
    {
        var previous = fixture.WaitForAssertionAsyncTimeout;
        fixture.WaitForAssertionAsyncTimeout = TimeSpan.FromMilliseconds(500);
        try
        {
            var grain = fixture.GetGrain<ICounterGrain>(Guid.NewGuid().ToString());

            var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
                fixture.WaitForAssertionAsync(
                    grain,
                    static async grain => Assert.Equal(999, await grain.GetValue())));

            Assert.IsNotType<TaskCanceledException>(ex);
            Assert.IsNotType<OperationCanceledException>(ex);
        }
        finally
        {
            fixture.WaitForAssertionAsyncTimeout = previous;
        }
    }
}