using System.Threading.Channels;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Event emitted by <see cref="GrainCallCollectionFilter"/> after each non-system,
/// non-self-triggered grain call completes (regardless of success or fault).
/// </summary>
/// <param name="TargetGrainId">The grain that received the call.</param>
/// <param name="InterfaceName">The fully-qualified grain interface name as reported by Orleans.</param>
/// <param name="MethodName">The method that was invoked.</param>
/// <param name="TestId">
/// The value of <c>RequestContext["test.id"]</c> at the time of the call, or <see langword="null"/>
/// if the call was not attributed to a test.
/// </param>
/// <param name="Timestamp">UTC timestamp captured immediately after the inner invocation returned.</param>
public readonly record struct GrainCallTriggerEvent(
    GrainId TargetGrainId,
    string InterfaceName,
    string MethodName,
    string? TestId,
    DateTimeOffset Timestamp);

/// <summary>
/// Orleans incoming grain call filter that broadcasts a <see cref="GrainCallTriggerEvent"/>
/// to every active subscriber after each grain call completes.
/// </summary>
/// <remarks>
/// <para>
/// Each subscriber gets its own bounded <see cref="Channel{T}"/> (capacity 4096) so concurrent
/// readers never compete for events. The filter snapshots the subscriber list outside the lock
/// before publishing so writes never block other subscribe/unsubscribe operations.
/// </para>
/// <para>
/// Channels are configured with <see cref="BoundedChannelFullMode.Wait"/>, but the publisher
/// uses <see cref="ChannelWriter{T}.TryWrite"/> and throws when a channel is full so test slots
/// fail loudly instead of silently dropping triggers.
/// </para>
/// <para>
/// Calls are always forwarded to the inner pipeline via <c>IIncomingGrainCallContext.Invoke</c>;
/// emission only happens after the inner call returns. Calls are skipped when:
/// <list type="bullet">
///   <item>The grain interface namespace starts with <c>Orleans.Runtime.</c>.</item>
///   <item><c>RequestContext["test-assertion"]</c> is <see langword="true"/>.</item>
///   <item>No subscribers are active (recording is reference-counted on subscriber count).</item>
/// </list>
/// Stream consumer extension calls (<c>Orleans.Streams.</c> namespace) are <em>not</em> skipped:
/// they represent real cross-grain message delivery and are needed to wake assertions waiting
/// on stream-driven fan-out.
/// </para>
/// <para>
/// Subscribers may optionally provide a <c>testId</c> filter and/or a
/// <see cref="Predicate{IIncomingGrainCallContext}"/> that runs at the silo edge before any
/// per-subscriber channel write.
/// </para>
/// </remarks>
public sealed class GrainCallCollectionFilter : IIncomingGrainCallFilter, IDisposable
{
    /// <summary>Default per-subscriber channel capacity.</summary>
    public const int SubscriberChannelCapacity = 4096;

    /// <summary>The <see cref="RequestContext"/> key used to mark assertion-machinery calls.</summary>
    private const string TestAssertionKey = "test-assertion";

    /// <summary>The <see cref="RequestContext"/> key used to attribute calls to a test id.</summary>
    private const string TestIdKey = "test.id";

    /// <summary>Guards <see cref="subscribers"/> list mutations.</summary>
    private readonly Lock subscribersLock = new();

    /// <summary>
    /// Immutable-by-convention snapshot of current subscribers. Swapped on subscribe/unsubscribe.
    /// </summary>
    private List<Subscriber> subscribers = [];

    /// <summary>Reference count of active subscribers for the hot-path check.</summary>
    private int subscriberCount;

    /// <summary>Fast volatile flag derived from <see cref="subscriberCount"/>.</summary>
    private volatile bool recordingEnabled;

    /// <summary>Tracks whether <see cref="Dispose"/> has been called.</summary>
    private volatile bool disposed;

    /// <summary>
    /// Subscribes for trigger events. The returned <see cref="IDisposable"/> unsubscribes
    /// the channel and completes its writer.
    /// </summary>
    /// <param name="reader">Receives the bounded channel reader the caller can read events from.</param>
    /// <param name="testId">
    /// Optional <c>test.id</c> filter; when supplied, only events whose <see cref="GrainCallTriggerEvent.TestId"/>
    /// equals this value are written to the channel. When <see langword="null"/>, all events are written.
    /// </param>
    /// <param name="filter">
    /// Optional silo-edge predicate. When supplied, only calls for which the predicate returns
    /// <see langword="true"/> are written to this subscriber's channel.
    /// </param>
    /// <returns>A disposable that, when disposed, unsubscribes and completes the channel.</returns>
    public IDisposable Subscribe(
        out ChannelReader<GrainCallTriggerEvent> reader,
        string? testId = null,
        Predicate<IIncomingGrainCallContext>? filter = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var channel = Channel.CreateBounded<GrainCallTriggerEvent>(new BoundedChannelOptions(SubscriberChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false,
        });

        var subscriber = new Subscriber(channel, testId, filter);
        lock (subscribersLock)
        {
            subscribers = [.. subscribers, subscriber];
            if (Interlocked.Increment(ref subscriberCount) > 0)
            {
                recordingEnabled = true;
            }
        }

        reader = channel.Reader;
        return new Subscription(this, subscriber);
    }

    /// <inheritdoc />
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var interfaceTypeText = context.InterfaceType.ToString() ?? string.Empty;
        var isSystemCall = interfaceTypeText.StartsWith("Orleans.Runtime.", StringComparison.Ordinal);
        var isAssertionScope = RequestContext.Get(TestAssertionKey) is true;
        var testId = RequestContext.Get(TestIdKey) as string;
        var interfaceName = context.InterfaceName;
        var methodName = context.MethodName;
        var targetGrainId = context.TargetId;

        System.Runtime.ExceptionServices.ExceptionDispatchInfo? failure = null;
        try
        {
            await context.Invoke().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            failure = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex);
        }

        if (recordingEnabled && !disposed && !isSystemCall && !isAssertionScope)
        {
            var triggerEvent = new GrainCallTriggerEvent(
                targetGrainId,
                interfaceName,
                methodName,
                testId,
                DateTimeOffset.UtcNow);

            // Snapshot reference is immutable — safe to iterate without holding the lock.
            var snapshot = subscribers;
            foreach (var subscriber in snapshot)
            {
                if (subscriber.TestId is not null && !string.Equals(subscriber.TestId, testId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (subscriber.Filter is not null && !subscriber.Filter(context))
                {
                    continue;
                }

                if (!subscriber.Channel.Writer.TryWrite(triggerEvent))
                {
                    throw new InvalidOperationException(
                        $"GrainCallCollectionFilter subscriber channel is full (capacity {SubscriberChannelCapacity}). " +
                        "The test slot is broken — a subscriber is not draining its events. " +
                        $"TestId='{subscriber.TestId ?? "<any>"}', Interface='{interfaceName}', Method='{methodName}'.");
                }
            }
        }

        failure?.Throw();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        List<Subscriber> snapshot;
        lock (subscribersLock)
        {
            snapshot = subscribers;
            subscribers = [];
            subscriberCount = 0;
            recordingEnabled = false;
        }

        foreach (var subscriber in snapshot)
        {
            subscriber.Channel.Writer.TryComplete();
        }
    }

    /// <summary>
    /// Removes a subscriber from the active list and completes its channel writer.
    /// </summary>
    /// <param name="subscriber">The subscriber to remove.</param>
    private void Unsubscribe(Subscriber subscriber)
    {
        lock (subscribersLock)
        {
            subscribers = [.. subscribers.Where(s => !ReferenceEquals(s, subscriber))];
            if (Interlocked.Decrement(ref subscriberCount) <= 0)
            {
                subscriberCount = 0;
                recordingEnabled = false;
            }
        }

        subscriber.Channel.Writer.TryComplete();
    }

    /// <summary>Holds a subscriber's channel, optional testId filter, and optional predicate filter.</summary>
    private sealed class Subscriber(
        Channel<GrainCallTriggerEvent> channel,
        string? testId,
        Predicate<IIncomingGrainCallContext>? filter)
    {
        /// <summary>The bounded channel for this subscriber.</summary>
        public Channel<GrainCallTriggerEvent> Channel { get; } = channel;

        /// <summary>Optional test id filter; null means accept all.</summary>
        public string? TestId { get; } = testId;

        /// <summary>Optional silo-edge predicate filter; null means accept all.</summary>
        public Predicate<IIncomingGrainCallContext>? Filter { get; } = filter;
    }

    /// <summary>
    /// Disposable subscription handle that unsubscribes on dispose.
    /// </summary>
    private sealed class Subscription(GrainCallCollectionFilter owner, Subscriber subscriber) : IDisposable
    {
        /// <summary>Tracks whether dispose has been called (0 = not disposed, 1 = disposed).</summary>
        private int disposed;

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                owner.Unsubscribe(subscriber);
            }
        }
    }
}