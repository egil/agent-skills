using System.Threading.Channels;
using Xunit;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Unit tests for <see cref="GrainCallCollectionFilter"/> covering subscriber lifecycle,
/// event broadcasting, filtering, system call exclusion, and error behavior.
/// </summary>
public sealed class GrainCallCollectionFilterTests : IDisposable
{
    /// <summary>The filter under test.</summary>
    private readonly GrainCallCollectionFilter sut = new();

    /// <inheritdoc />
    public void Dispose() => sut.Dispose();

    /// <summary>Creates a fake grain call context with configurable properties.</summary>
    private static FakeIncomingGrainCallContext MakeContext(
        string interfaceTypeName = "Some.App.IFooGrain",
        string interfaceName = "Some.App.IFooGrain",
        string methodName = "DoWork",
        string grainKey = "g1",
        Func<Task>? onInvoke = null)
        => new()
        {
            InterfaceType = new GrainInterfaceType(interfaceTypeName),
            InterfaceName = interfaceName,
            MethodName = methodName,
            TargetId = GrainId.Create("foo", grainKey),
            OnInvoke = onInvoke,
        };

    /// <summary>Drains a channel reader, collecting up to <paramref name="expectedCount"/> events.</summary>
    private static async Task<List<GrainCallTriggerEvent>> DrainAsync(
        ChannelReader<GrainCallTriggerEvent> reader,
        int expectedCount,
        TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(2);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(actualTimeout);
        var events = new List<GrainCallTriggerEvent>(expectedCount);
        try
        {
            while (events.Count < expectedCount)
            {
                events.Add(await reader.ReadAsync(cts.Token));
            }
        }
        catch (OperationCanceledException)
        {
            // Return what we collected for diagnostics.
        }

        return events;
    }

    /// <summary>Forwards inner call when no subscribers are active.</summary>
    [Fact]
    public async Task Forwards_inner_call_when_no_subscribers()
    {
        var ctx = MakeContext();
        await sut.Invoke(ctx);
        Assert.Equal(1, ctx.InvokeCount);
    }

    /// <summary>Forwards inner call when a subscriber is active.</summary>
    [Fact]
    public async Task Forwards_inner_call_when_subscriber_active()
    {
        using var sub = sut.Subscribe(out _);
        var ctx = MakeContext();
        await sut.Invoke(ctx);
        Assert.Equal(1, ctx.InvokeCount);
    }

    /// <summary>Broadcasts events to all current subscribers.</summary>
    [Fact]
    public async Task Broadcasts_event_to_all_current_subscribers()
    {
        using var subA = sut.Subscribe(out var readerA);
        using var subB = sut.Subscribe(out var readerB);

        await sut.Invoke(MakeContext(methodName: "Hello"));

        var aEvents = await DrainAsync(readerA, 1);
        var bEvents = await DrainAsync(readerB, 1);

        Assert.Single(aEvents);
        Assert.Equal("Hello", aEvents[0].MethodName);
        Assert.Single(bEvents);
        Assert.Equal("Hello", bEvents[0].MethodName);
    }

    /// <summary>Filters events per subscriber test id.</summary>
    [Fact]
    public async Task Filters_events_per_subscriber_test_id()
    {
        using var subA = sut.Subscribe(out var readerA, testId: "test-A");
        using var subB = sut.Subscribe(out var readerB, testId: "test-B");
        using var subAll = sut.Subscribe(out var readerAll);

        using (RequestContextScope.ForTest("test-A"))
        {
            await sut.Invoke(MakeContext(methodName: "FromA"));
        }

        var aEvents = await DrainAsync(readerA, 1);
        var allEvents = await DrainAsync(readerAll, 1);

        Assert.Single(aEvents);
        Assert.Equal("test-A", aEvents[0].TestId);
        Assert.Single(allEvents);

        // Subscriber B should not receive the event for test-A.
        Assert.False(readerB.TryRead(out _));
    }

    /// <summary>Suppresses events made inside an assertion scope.</summary>
    [Fact]
    public async Task Suppresses_events_made_inside_assertion_scope()
    {
        using var sub = sut.Subscribe(out var reader);

        using (RequestContextScope.ForAssertion())
        {
            await sut.Invoke(MakeContext(methodName: "ShouldNotEmit"));
        }

        await Task.Delay(20, TestContext.Current.CancellationToken);
        Assert.False(reader.TryRead(out _));
    }

    /// <summary>Skips Orleans runtime system grain calls.</summary>
    [Fact]
    public async Task Skips_system_grain_namespaces()
    {
        using var sub = sut.Subscribe(out var reader);

        await sut.Invoke(MakeContext(interfaceTypeName: "Orleans.Runtime.IManagementGrain", methodName: "Internal"));

        await Task.Delay(20, TestContext.Current.CancellationToken);
        Assert.False(reader.TryRead(out _));
    }

    /// <summary>Stream consumer extension calls are emitted (not suppressed).</summary>
    [Fact]
    public async Task Stream_consumer_extension_calls_are_emitted()
    {
        using var sub = sut.Subscribe(out var reader);

        await sut.Invoke(MakeContext(
            interfaceTypeName: "Orleans.Streams.IStreamConsumerExtension",
            methodName: "DeliverItem"));

        var ev = await reader.ReadAsync(TestContext.Current.CancellationToken);
        Assert.Equal("DeliverItem", ev.MethodName);
    }

    /// <summary>Bounded channel overflow throws a clear exception.</summary>
    [Fact]
    public async Task Bounded_channel_overflow_throws_clear_exception()
    {
        using var sub = sut.Subscribe(out _);

        for (var i = 0; i < GrainCallCollectionFilter.SubscriberChannelCapacity; i++)
        {
            await sut.Invoke(MakeContext(methodName: $"M{i}"));
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Invoke(MakeContext(methodName: "Overflow")));
        Assert.Contains("subscriber channel is full", ex.Message);
    }

    /// <summary>Recording enables on first subscribe and disables on last unsubscribe.</summary>
    [Fact]
    public async Task Recording_enables_on_first_subscribe_and_disables_on_last_unsubscribe()
    {
        var sub1 = sut.Subscribe(out var reader1);
        await sut.Invoke(MakeContext(methodName: "On1"));
        var first = await DrainAsync(reader1, 1);
        Assert.Single(first);
        Assert.Equal("On1", first[0].MethodName);

        sub1.Dispose();
        await sut.Invoke(MakeContext(methodName: "OffBetween"));

        using var sub2 = sut.Subscribe(out var reader2);
        await sut.Invoke(MakeContext(methodName: "On2"));
        var second = await DrainAsync(reader2, 1);
        Assert.Single(second);
        Assert.Equal("On2", second[0].MethodName);
    }

    /// <summary>Emits event even when the inner call throws.</summary>
    [Fact]
    public async Task Emits_event_even_when_inner_call_throws()
    {
        using var sub = sut.Subscribe(out var reader);

        var failingCtx = MakeContext(
            methodName: "Boom",
            onInvoke: () => throw new InvalidOperationException("boom"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Invoke(failingCtx));

        var events = await DrainAsync(reader, 1);
        Assert.Single(events);
        Assert.Equal("Boom", events[0].MethodName);
    }

    /// <summary>Unsubscribed subscriber no longer receives events.</summary>
    [Fact]
    public async Task Unsubscribed_subscriber_no_longer_receives_events()
    {
        var sub = sut.Subscribe(out var reader);
        sub.Dispose();

        await sut.Invoke(MakeContext(methodName: "AfterUnsub"));

        Assert.False(reader.TryRead(out _));
        Assert.True(reader.Completion.IsCompleted);
    }

    /// <summary>Subscriber predicate filter drops non-matching events at silo edge.</summary>
    [Fact]
    public async Task Subscriber_predicate_filter_drops_non_matching_events()
    {
        var targetGrain = GrainId.Create("foo", "wanted");
        using var sub = sut.Subscribe(
            out var reader,
            filter: ctx => ctx.TargetId == targetGrain);

        await sut.Invoke(MakeContext(methodName: "Other", grainKey: "ignored"));
        await sut.Invoke(MakeContext(methodName: "Wanted", grainKey: "wanted"));

        var events = await DrainAsync(reader, 1);
        Assert.Single(events);
        Assert.Equal("Wanted", events[0].MethodName);
        Assert.False(reader.TryRead(out _));
    }

    /// <summary>Subscriber predicate filter combines with test id filter.</summary>
    [Fact]
    public async Task Subscriber_predicate_filter_combines_with_test_id_filter()
    {
        var targetGrain = GrainId.Create("foo", "wanted");
        using var sub = sut.Subscribe(
            out var reader,
            testId: "test-A",
            filter: ctx => ctx.TargetId == targetGrain);

        using (RequestContextScope.ForTest("test-B"))
        {
            await sut.Invoke(MakeContext(methodName: "WrongTest", grainKey: "wanted"));
        }

        using (RequestContextScope.ForTest("test-A"))
        {
            await sut.Invoke(MakeContext(methodName: "WrongGrain", grainKey: "ignored"));
        }

        using (RequestContextScope.ForTest("test-A"))
        {
            await sut.Invoke(MakeContext(methodName: "Hit", grainKey: "wanted"));
        }

        var events = await DrainAsync(reader, 1);
        Assert.Single(events);
        Assert.Equal("Hit", events[0].MethodName);
        Assert.False(reader.TryRead(out _));
    }
}
