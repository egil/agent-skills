namespace OrleansAsyncAssertion.Grains;

/// <summary>
/// Publishes values to an Orleans stream.
/// </summary>
public interface IStreamProducerGrain : IGrainWithStringKey
{
    /// <summary>
    /// Publishes a value to the configured stream.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="value">The payload value.</param>
    /// <returns>A task that completes when the value is published.</returns>
    Task Publish(Guid streamId, int value);
}
