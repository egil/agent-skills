using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Represents a position in the in-memory activity stream.
/// </summary>
public readonly record struct ActivityCursor(long Sequence);

/// <summary>
/// Collects trace activities in memory and exposes them as a replayable async stream.
/// </summary>
public sealed class TestTraceCollector : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Limits the in-memory activity buffer to a fixed size.
    /// </summary>
    private const int BufferLimit = 1024;

    /// <summary>
    /// Collects activities as they are exported.
    /// </summary>
    private readonly Channel<ActivityEnvelope> allActivities = Channel.CreateUnbounded<ActivityEnvelope>(
        new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
        });

    /// <summary>
    /// Synchronizes access to the replay buffer.
    /// </summary>
    private readonly Lock sync = new();

    /// <summary>
    /// Stores recent activities for replay.
    /// </summary>
    private readonly List<ActivityEnvelope> buffer = new();

    /// <summary>
    /// The tracer provider used to subscribe to activity sources.
    /// </summary>
    private readonly TracerProvider tracerProvider;

    /// <summary>
    /// The processor that forwards activities to the in-memory exporter.
    /// </summary>
    private readonly SimpleActivityExportProcessor processor;

    /// <summary>
    /// The streaming exporter wrapper.
    /// </summary>
    private readonly StreamingInMemoryExporter streamingExporter;

    /// <summary>
    /// Tracks the sequence number for activities.
    /// </summary>
    private long sequence;

    /// <summary>
    /// Starts listening to the provided activity sources.
    /// </summary>
    /// <param name="activitySources">The activity source names to listen to.</param>
    public TestTraceCollector(IEnumerable<string> activitySources)
    {
        var exporter = new InMemoryExporter<Activity>(new List<Activity>());
        streamingExporter = new StreamingInMemoryExporter(exporter, OnActivityStopped);
        processor = new SimpleActivityExportProcessor(streamingExporter);
        var builder = Sdk.CreateTracerProviderBuilder()
            .SetSampler(new AlwaysOnSampler())
            .AddProcessor(processor);
        foreach (var source in activitySources)
        {
            builder.AddSource(source);
        }

        tracerProvider = builder.Build();
    }

    /// <summary>
    /// Returns a cursor that starts after the most recently observed activity.
    /// </summary>
    /// <returns>A cursor positioned after the latest activity.</returns>
    public ActivityCursor GetCursor() => new(Interlocked.Read(ref sequence) + 1);

    /// <summary>
    /// Streams activities starting from the supplied cursor position.
    /// </summary>
    /// <param name="cursor">The starting cursor.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An async stream of activities.</returns>
    public async IAsyncEnumerable<Activity> GetActivities(
        ActivityCursor cursor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        List<ActivityEnvelope> snapshot;
        lock (sync)
        {
            snapshot = buffer
                .Where(envelope => envelope.Sequence >= cursor.Sequence)
                .ToList();
        }

        var minSequence = cursor.Sequence;
        foreach (var envelope in snapshot)
        {
            if (envelope.Sequence >= minSequence)
            {
                yield return envelope.Activity;
            }
        }

        if (snapshot.Count > 0)
        {
            minSequence = Math.Max(minSequence, snapshot[^1].Sequence + 1);
        }

        await foreach (var envelope in allActivities.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (envelope.Sequence < minSequence)
            {
                continue;
            }

            yield return envelope.Activity;
        }
    }

    /// <summary>
    /// Records a completed activity into the replay buffer and stream.
    /// </summary>
    /// <param name="activity">The completed activity.</param>
    private void OnActivityStopped(Activity activity)
    {
        var nextSequence = Interlocked.Increment(ref sequence);
        var envelope = new ActivityEnvelope(nextSequence, activity);

        lock (sync)
        {
            buffer.Add(envelope);
            if (buffer.Count > BufferLimit)
            {
                buffer.RemoveRange(0, buffer.Count - BufferLimit);
            }
        }

        allActivities.Writer.TryWrite(envelope);
    }

    /// <summary>
    /// Stops listening and completes the activity stream.
    /// </summary>
    public void Dispose()
    {
        allActivities.Writer.TryComplete();
        tracerProvider.Dispose();
        processor.Dispose();
        streamingExporter.Dispose();
    }

    /// <summary>
    /// Asynchronously disposes the activity collector.
    /// </summary>
    /// <returns>A completed value task.</returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Wraps an activity with its sequence number for ordered replay.
    /// </summary>
    private sealed record ActivityEnvelope(long Sequence, Activity Activity);

    /// <summary>
    /// Wraps the OpenTelemetry in-memory exporter to emit activities into the async stream.
    /// </summary>
    private sealed class StreamingInMemoryExporter : BaseExporter<Activity>
    {
        /// <summary>
        /// The underlying in-memory exporter.
        /// </summary>
        private readonly InMemoryExporter<Activity> inner;

        /// <summary>
        /// The callback invoked for each exported activity.
        /// </summary>
        private readonly Action<Activity> onExport;

        /// <summary>
        /// Creates a streaming wrapper for an in-memory exporter.
        /// </summary>
        /// <param name="inner">The inner exporter.</param>
        /// <param name="onExport">The callback to invoke.</param>
        public StreamingInMemoryExporter(InMemoryExporter<Activity> inner, Action<Activity> onExport)
        {
            this.inner = inner;
            this.onExport = onExport;
        }

        /// <summary>
        /// Emits exported activities into the async stream and the in-memory buffer.
        /// </summary>
        /// <param name="batch">The batch of activities.</param>
        /// <returns>The export result.</returns>
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch)
            {
                onExport(activity);
            }

            return inner.Export(batch);
        }
    }
}
