namespace OrleansAsyncAssertion.Grains;

/// <summary>
/// Receives values from an Orleans stream and records the last observed value.
/// </summary>
public interface IStreamConsumerGrain : IGrainWithStringKey
{
    /// <summary>
    /// Subscribes to the stream identified by the supplied ID.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <returns>A task that completes when the subscription is active.</returns>
    Task Subscribe(Guid streamId);

    /// <summary>
    /// Reads the last value delivered to this consumer.
    /// </summary>
    /// <returns>The last delivered value.</returns>
    Task<int> GetLastValue();
}
