using OrleansAsyncAssertion.Grains;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Defines the activity sources used by the test collector.
/// </summary>
public static class TestTraceSources
{
    /// <summary>
    /// Gets the list of activity source names to collect.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = new[]
    {
        "Microsoft.Orleans.Runtime",
        "Microsoft.Orleans.Application",
        StreamConsumerGrain.StreamActivitySource.Name,
    };
}
