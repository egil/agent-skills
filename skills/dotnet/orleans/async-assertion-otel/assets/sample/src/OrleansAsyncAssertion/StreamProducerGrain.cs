namespace OrleansAsyncAssertion.Grains;

/// <summary>
/// Implements a grain that publishes values to a stream provider.
/// </summary>
public sealed class StreamProducerGrain : Grain, IStreamProducerGrain
{
    /// <summary>
    /// Publishes a value to the configured stream.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="value">The payload value.</param>
    /// <returns>A task that completes when the value is published.</returns>
    public async Task Publish(Guid streamId, int value)
    {
        var stream = Orleans.GrainStreamingExtensions.GetStreamProvider(this, StreamConstants.StreamProviderName)
            .GetStream<int>(Orleans.Runtime.StreamId.Create(StreamConstants.StreamNamespace, streamId));
        await stream.OnNextAsync(value);
    }
}
