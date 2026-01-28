using System.Diagnostics;
using Orleans.Streams;

namespace OrleansAsyncAssertion.Grains;

/// <summary>
/// Consumes stream values and emits a tracing activity when delivery completes.
/// </summary>
public sealed class StreamConsumerGrain : Grain, IStreamConsumerGrain, IAsyncObserver<int>
{
    /// <summary>
    /// The activity source used to tag stream delivery events.
    /// </summary>
    public static readonly ActivitySource StreamActivitySource = new("OrleansAsyncAssertion.Streams");

    /// <summary>
    /// Stores the current subscription handle.
    /// </summary>
    private StreamSubscriptionHandle<int>? subscription;

    /// <summary>
    /// Tracks the last received value.
    /// </summary>
    private int lastValue;

    /// <summary>
    /// Stores the stream identifier to tag delivery spans.
    /// </summary>
    private Guid streamId;

    /// <summary>
    /// Stores the test identifier used to correlate spans.
    /// </summary>
    private string? testId;

    /// <summary>
    /// Subscribes to the stream identified by the supplied ID.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <returns>A task that completes when the subscription is active.</returns>
    public async Task Subscribe(Guid streamId)
    {
        if (subscription is not null)
        {
            await subscription.UnsubscribeAsync();
        }

        this.streamId = streamId;
        var stream = Orleans.GrainStreamingExtensions.GetStreamProvider(this, StreamConstants.StreamProviderName)
            .GetStream<int>(Orleans.Runtime.StreamId.Create(StreamConstants.StreamNamespace, streamId));
        subscription = await stream.SubscribeAsync(this);
    }

    /// <summary>
    /// Sets the test identifier used to tag stream delivery spans.
    /// </summary>
    /// <param name="testId">The unique test identifier.</param>
    /// <returns>A task that completes when the identifier is stored.</returns>
    public Task SetTestId(string testId)
    {
        this.testId = testId;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads the last value delivered to this consumer.
    /// </summary>
    /// <returns>The last delivered value.</returns>
    public Task<int> GetLastValue()
    {
        return Task.FromResult(lastValue);
    }

    /// <summary>
    /// Handles a stream delivery.
    /// </summary>
    /// <param name="item">The delivered value.</param>
    /// <param name="token">The optional stream sequence token.</param>
    /// <returns>A task that completes when the value is processed.</returns>
    public Task OnNextAsync(int item, StreamSequenceToken? token = null)
    {
        lastValue = item;
        using var activity = StreamActivitySource.StartActivity("StreamDelivery", ActivityKind.Consumer);
        if (activity is not null)
        {
            activity.SetTag("stream.namespace", StreamConstants.StreamNamespace);
            activity.SetTag("stream.id", streamId.ToString("N"));
            if (!string.IsNullOrWhiteSpace(testId))
            {
                activity.SetTag("test.id", testId);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles a stream completion signal.
    /// </summary>
    /// <returns>A task that completes when the completion is processed.</returns>
    public Task OnCompletedAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles a stream error.
    /// </summary>
    /// <param name="ex">The exception describing the error.</param>
    /// <returns>A task that completes when the error is processed.</returns>
    public Task OnErrorAsync(Exception ex)
    {
        return Task.CompletedTask;
    }
}
