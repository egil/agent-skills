using System.Threading.Channels;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// An incoming grain call filter that records completed grain calls into a bounded channel
/// for use by test assertion helpers. Assertion calls and Orleans internal calls are excluded.
/// </summary>
public sealed class GrainCallCollectionFilter : IIncomingGrainCallFilter, IDisposable
{
    private readonly Channel<IIncomingGrainCallContext> allActivities = Channel.CreateBounded<IIncomingGrainCallContext>(
        new BoundedChannelOptions(1_000)
        {
            SingleReader = false, SingleWriter = true, Capacity = 1_000, FullMode = BoundedChannelFullMode.DropOldest,
        });

    private bool collectionEnabled;

    /// <summary>
    /// Returns an async stream of grain call contexts recorded by this filter.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An async enumerable of grain call contexts.</returns>
    public IAsyncEnumerable<IIncomingGrainCallContext> GetCalls(CancellationToken cancellationToken)
        => allActivities.Reader.ReadAllAsync(cancellationToken);

    /// <summary>
    /// Invokes the grain call and records it into the channel unless it is an internal
    /// Orleans call or an assertion call.
    /// </summary>
    /// <param name="context">The incoming grain call context.</param>
    /// <returns>A task that completes when the call and recording are finished.</returns>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        await context.Invoke();

        if (!collectionEnabled || IsOrleansInternalCall(context) || RequestContext.Get("test-assertion") is true)
        {
            return;
        }

        await allActivities.Writer.WriteAsync(context);
    }

    /// <summary>
    /// Completes the channel so consumers stop reading.
    /// </summary>
    public void Dispose()
    {
        allActivities.Writer.TryComplete();
    }

    /// <summary>
    /// Enables recording of grain calls. Call after the cluster is deployed.
    /// </summary>
    public void EnableRecording()
    {
        collectionEnabled = true;
    }

    /// <summary>
    /// Determines whether the grain call is an Orleans internal call that should be ignored.
    /// </summary>
    /// <param name="context">The incoming grain call context.</param>
    /// <returns><c>true</c> if the call is internal; otherwise <c>false</c>.</returns>
    private static bool IsOrleansInternalCall(IIncomingGrainCallContext context)
    {
        return context.InterfaceType.ToString().StartsWith("Orleans.Runtime.", StringComparison.Ordinal);
    }
}