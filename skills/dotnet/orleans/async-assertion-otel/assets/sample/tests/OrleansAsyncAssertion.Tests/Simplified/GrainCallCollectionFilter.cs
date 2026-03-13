using System.Threading.Channels;

namespace OrleansAsyncAssertion.Tests.Simplified;

public sealed class GrainCallCollectionFilter : IIncomingGrainCallFilter, IDisposable
{
    private readonly Channel<IIncomingGrainCallContext> allActivities = Channel.CreateBounded<IIncomingGrainCallContext>(
        new BoundedChannelOptions(1_000)
        {
            SingleReader = false, SingleWriter = true, Capacity = 1_000, FullMode = BoundedChannelFullMode.DropOldest,
        });

    private bool collectionEnabled;

    public IAsyncEnumerable<IIncomingGrainCallContext> GetCalls(CancellationToken cancellationToken)
        => allActivities.Reader.ReadAllAsync(cancellationToken);

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        await context.Invoke();

        if (!collectionEnabled || IsOrleansInternalCall(context) || RequestContext.Get("test-assertion") is true)
        {
            return;
        }

        await allActivities.Writer.WriteAsync(context);
    }

    public void Dispose()
    {
        allActivities.Writer.TryComplete();
    }

    public void EnableRecording()
    {
        collectionEnabled = true;
    }

    private static bool IsOrleansInternalCall(IIncomingGrainCallContext context)
    {
        return context.InterfaceType.ToString().StartsWith("Orleans.Runtime.", StringComparison.Ordinal);
    }
}