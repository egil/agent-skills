using Orleans.Concurrency;

namespace OrleansAsyncAssertion.Grains;

/// <summary>
/// Defines a simple counter grain used to demonstrate RPC tracing.
/// </summary>
public interface ICounterGrain : IGrainWithStringKey
{
    /// <summary>
    /// Adds the supplied value to the counter and returns the updated total.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    /// <returns>The updated counter value.</returns>
    Task<int> Add(int amount);

    /// <summary>
    /// Adds the supplied value using a one-way RPC call.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    /// <returns>A task that completes when the one-way call is scheduled.</returns>
    [OneWay]
    Task AddOneWay(int amount);

    /// <summary>
    /// Reads the current counter value.
    /// </summary>
    /// <returns>The current counter value.</returns>
    Task<int> GetValue();
}
