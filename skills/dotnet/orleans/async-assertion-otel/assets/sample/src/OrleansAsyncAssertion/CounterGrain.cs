namespace OrleansAsyncAssertion.Grains;

/// <summary>
/// Implements a counter grain with RPC methods to update and read the state.
/// </summary>
public sealed class CounterGrain : Grain, ICounterGrain
{
    /// <summary>
    /// Holds the current counter value in memory.
    /// </summary>
    private int value;

    /// <summary>
    /// Adds the supplied value to the counter and returns the updated total.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    /// <returns>The updated counter value.</returns>
    public Task<int> Add(int amount)
    {
        value += amount;
        return Task.FromResult(value);
    }

    /// <summary>
    /// Adds the supplied value using a one-way RPC call.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    /// <returns>A task that completes when the one-way call is scheduled.</returns>
    public Task AddOneWay(int amount)
    {
        value += amount;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads the current counter value.
    /// </summary>
    /// <returns>The current counter value.</returns>
    public Task<int> GetValue()
    {
        return Task.FromResult(value);
    }
}
