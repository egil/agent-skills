using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace OrleansServiceObserver.Tests;

/// <summary>
/// Represents a position in the in-memory activity stream.
/// </summary>
public readonly record struct ActivityCursor(long Sequence);

/// <summary>
/// Collects Orleans trace activities in memory and exposes them as an async stream.
/// </summary>
public sealed class TestTraceCollector : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Limits the in-memory activity buffer to a fixed size.
    /// </summary>
    private const int BufferLimit = 1024;

    private readonly Channel<ActivityEnvelope> allActivities = Channel.CreateUnbounded<ActivityEnvelope>(
        new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
        });

    private readonly Lock sync = new();

    private readonly List<ActivityEnvelope> buffer = new();

    private readonly TracerProvider tracerProvider;

    private readonly SimpleActivityExportProcessor processor;

    private readonly StreamingInMemoryExporter streamingExporter;

    private long sequence;

    /// <summary>
    /// Starts listening to Orleans activity sources.
    /// </summary>
    public TestTraceCollector()
    {
        var exporter = new InMemoryExporter<Activity>(new List<Activity>());
        streamingExporter = new StreamingInMemoryExporter(exporter, OnActivityStopped);
        processor = new SimpleActivityExportProcessor(streamingExporter);
        tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("Microsoft.Orleans.Runtime", "Microsoft.Orleans.Application")
            .SetSampler(new AlwaysOnSampler())
            .AddProcessor(processor)
            .Build();
    }

    /// <summary>
    /// Returns a cursor that starts after the most recently observed activity.
    /// </summary>
    public ActivityCursor GetCursor() => new(Interlocked.Read(ref sequence) + 1);

    /// <summary>
    /// Streams activities starting from the supplied cursor position.
    /// </summary>
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

        await foreach (var envelope in allActivities.Reader.ReadAllAsync(cancellationToken))
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
    private void OnActivityStopped(Activity activity)
    {
        var nextSequence = Interlocked.Increment(ref this.sequence);
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
        private readonly InMemoryExporter<Activity> inner;
        private readonly Action<Activity> onExport;

        /// <summary>
        /// Creates a streaming wrapper for an in-memory exporter.
        /// </summary>
        public StreamingInMemoryExporter(InMemoryExporter<Activity> inner, Action<Activity> onExport)
        {
            this.inner = inner;
            this.onExport = onExport;
        }

        /// <summary>
        /// Emits exported activities into the async stream and the in-memory buffer.
        /// </summary>
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