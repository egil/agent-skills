using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Xunit;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Provides <c>WaitForAssertionAsync</c> extension methods on any
/// <see cref="IGrainCallCollectorProvider"/> implementation (e.g. test fixtures).
/// </summary>
public static class GrainCallCollectorProviderExtensions
{
    extension(IGrainCallCollectorProvider provider)
    {
        // ---- Flavor A: already-resolved grain ----

        /// <summary>
        /// Waits for the assertion to pass by re-running it whenever a grain call targeting the
        /// specified grain completes. Uses a default trigger that matches the grain's <see cref="GrainId"/>.
        /// </summary>
        /// <typeparam name="TGrain">The grain type.</typeparam>
        /// <param name="grain">The grain whose calls trigger assertion retries.</param>
        /// <param name="assertion">The assertion to execute.</param>
        /// <returns>A task that completes when the assertion passes.</returns>
        [StackTraceHidden]
        public Task WaitForAssertionAsync<TGrain>(
            TGrain grain,
            Func<TGrain, Task> assertion)
            where TGrain : IGrain
            => provider.WaitForAssertionAsync(grain, DefaultTriggerFor(grain), assertion);

        /// <summary>
        /// Waits for the assertion to pass, returning a value. Uses a default trigger that matches
        /// the grain's <see cref="GrainId"/>.
        /// </summary>
        /// <typeparam name="TGrain">The grain type.</typeparam>
        /// <typeparam name="TOutput">The type returned by the assertion.</typeparam>
        /// <param name="grain">The grain whose calls trigger assertion retries.</param>
        /// <param name="assertion">The assertion to execute.</param>
        /// <returns>A task containing the assertion result.</returns>
        [StackTraceHidden]
        public Task<TOutput> WaitForAssertionAsync<TGrain, TOutput>(
            TGrain grain,
            Func<TGrain, Task<TOutput>> assertion)
            where TGrain : IGrain
            => provider.WaitForAssertionAsync(grain, DefaultTriggerFor(grain), assertion);

        /// <summary>
        /// Waits for the assertion to pass using a custom trigger predicate that determines
        /// which grain calls should cause a retry.
        /// </summary>
        /// <typeparam name="TGrain">The grain type.</typeparam>
        /// <param name="grain">The grain to pass to the assertion.</param>
        /// <param name="trigger">Predicate that filters which grain calls trigger a retry.</param>
        /// <param name="assertion">The assertion to execute.</param>
        /// <returns>A task that completes when the assertion passes.</returns>
        [StackTraceHidden]
        public Task WaitForAssertionAsync<TGrain>(
            TGrain grain,
            Predicate<IIncomingGrainCallContext> trigger,
            Func<TGrain, Task> assertion)
            where TGrain : IGrain
        {
            var ct = TestContext.Current.CancellationToken;
            return WaitForAssertionAsyncCore<TGrain, bool>(provider, grain, trigger, async g =>
                {
                    await assertion(g);
                    return true;
                },
                ct);
        }

        /// <summary>
        /// Waits for the assertion to pass using a custom trigger predicate, returning a value.
        /// </summary>
        /// <typeparam name="TGrain">The grain type.</typeparam>
        /// <typeparam name="TOutput">The type returned by the assertion.</typeparam>
        /// <param name="grain">The grain to pass to the assertion.</param>
        /// <param name="trigger">Predicate that filters which grain calls trigger a retry.</param>
        /// <param name="assertion">The assertion to execute.</param>
        /// <returns>A task containing the assertion result.</returns>
        [StackTraceHidden]
        public Task<TOutput> WaitForAssertionAsync<TGrain, TOutput>(
            TGrain grain,
            Predicate<IIncomingGrainCallContext> trigger,
            Func<TGrain, Task<TOutput>> assertion)
            where TGrain : IGrain
        {
            var ct = TestContext.Current.CancellationToken;
            return WaitForAssertionAsyncCore(provider, grain, trigger, assertion, ct);
        }

        // ---- Flavor B: id-types matrix (mirrors IGrainFactory.GetGrain) ----

        /// <summary>
        /// Resolves a grain by string key and waits for the assertion to pass.
        /// </summary>
        /// <typeparam name="TGrainInterface">The grain interface type.</typeparam>
        /// <param name="primaryKey">The grain's string primary key.</param>
        /// <param name="assertion">The assertion to execute.</param>
        /// <returns>A task that completes when the assertion passes.</returns>
        [StackTraceHidden]
        public Task WaitForAssertionAsync<TGrainInterface>(string primaryKey, Func<TGrainInterface, Task> assertion)
            where TGrainInterface : IGrainWithStringKey
            => provider.WaitForAssertionAsync(provider.GetGrain<TGrainInterface>(primaryKey), assertion);

        /// <summary>
        /// Resolves a grain by string key and waits for the assertion to pass, returning a value.
        /// </summary>
        /// <typeparam name="TGrainInterface">The grain interface type.</typeparam>
        /// <typeparam name="TOutput">The type returned by the assertion.</typeparam>
        /// <param name="primaryKey">The grain's string primary key.</param>
        /// <param name="assertion">The assertion to execute.</param>
        /// <returns>A task containing the assertion result.</returns>
        [StackTraceHidden]
        public Task<TOutput> WaitForAssertionAsync<TGrainInterface, TOutput>(string primaryKey, Func<TGrainInterface, Task<TOutput>> assertion)
            where TGrainInterface : IGrainWithStringKey
            => provider.WaitForAssertionAsync<TGrainInterface, TOutput>(provider.GetGrain<TGrainInterface>(primaryKey), assertion);

        /// <summary>
        /// Resolves a grain by <see cref="Guid"/> key and waits for the assertion to pass.
        /// </summary>
        /// <typeparam name="TGrainInterface">The grain interface type.</typeparam>
        /// <param name="primaryKey">The grain's <see cref="Guid"/> primary key.</param>
        /// <param name="assertion">The assertion to execute.</param>
        /// <returns>A task that completes when the assertion passes.</returns>
        [StackTraceHidden]
        public Task WaitForAssertionAsync<TGrainInterface>(Guid primaryKey, Func<TGrainInterface, Task> assertion)
            where TGrainInterface : IGrainWithGuidKey
            => provider.WaitForAssertionAsync(provider.GetGrain<TGrainInterface>(primaryKey), assertion);

        /// <summary>
        /// Resolves a grain by <see cref="Guid"/> key and waits for the assertion to pass, returning a value.
        /// </summary>
        /// <typeparam name="TGrainInterface">The grain interface type.</typeparam>
        /// <typeparam name="TOutput">The type returned by the assertion.</typeparam>
        /// <param name="primaryKey">The grain's <see cref="Guid"/> primary key.</param>
        /// <param name="assertion">The assertion to execute.</param>
        /// <returns>A task containing the assertion result.</returns>
        [StackTraceHidden]
        public Task<TOutput> WaitForAssertionAsync<TGrainInterface, TOutput>(Guid primaryKey, Func<TGrainInterface, Task<TOutput>> assertion)
            where TGrainInterface : IGrainWithGuidKey
            => provider.WaitForAssertionAsync<TGrainInterface, TOutput>(provider.GetGrain<TGrainInterface>(primaryKey), assertion);

        /// <summary>
        /// Resolves a grain by <see cref="long"/> key and waits for the assertion to pass.
        /// </summary>
        /// <typeparam name="TGrainInterface">The grain interface type.</typeparam>
        /// <param name="primaryKey">The grain's <see cref="long"/> primary key.</param>
        /// <param name="assertion">The assertion to execute.</param>
        /// <returns>A task that completes when the assertion passes.</returns>
        [StackTraceHidden]
        public Task WaitForAssertionAsync<TGrainInterface>(long primaryKey, Func<TGrainInterface, Task> assertion)
            where TGrainInterface : IGrainWithIntegerKey
            => provider.WaitForAssertionAsync(provider.GetGrain<TGrainInterface>(primaryKey), assertion);

        /// <summary>
        /// Resolves a grain by <see cref="long"/> key and waits for the assertion to pass, returning a value.
        /// </summary>
        /// <typeparam name="TGrainInterface">The grain interface type.</typeparam>
        /// <typeparam name="TOutput">The type returned by the assertion.</typeparam>
        /// <param name="primaryKey">The grain's <see cref="long"/> primary key.</param>
        /// <param name="assertion">The assertion to execute.</param>
        /// <returns>A task containing the assertion result.</returns>
        [StackTraceHidden]
        public Task<TOutput> WaitForAssertionAsync<TGrainInterface, TOutput>(long primaryKey, Func<TGrainInterface, Task<TOutput>> assertion)
            where TGrainInterface : IGrainWithIntegerKey
            => provider.WaitForAssertionAsync<TGrainInterface, TOutput>(provider.GetGrain<TGrainInterface>(primaryKey), assertion);
    }

    /// <summary>
    /// Builds a default trigger predicate that matches calls to the specified grain's <see cref="GrainId"/>.
    /// </summary>
    private static Predicate<IIncomingGrainCallContext> DefaultTriggerFor<TGrain>(TGrain grain)
        where TGrain : IGrain
    {
        var targetGrainId = grain.GetGrainId();
        return ctx => ctx.TargetId == targetGrainId;
    }

    /// <summary>
    /// Core implementation that subscribes to the call collector, tries the assertion immediately,
    /// then enters a channel-driven retry loop until the assertion passes or the timeout fires.
    /// </summary>
    [StackTraceHidden]
    private static Task<TOutput> WaitForAssertionAsyncCore<TGrain, TOutput>(
        IGrainCallCollectorProvider provider,
        TGrain grain,
        Predicate<IIncomingGrainCallContext> trigger,
        Func<TGrain, Task<TOutput>> assertion,
        CancellationToken cancellationToken)
        where TGrain : IGrain
    {
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(assertion);

        var lastFailureRef = new StrongBox<Exception?>(null);
        var loopTask = WaitForAssertionAsyncLoop(provider, grain, trigger, assertion, lastFailureRef, cancellationToken);

        return loopTask
            .ContinueWith(continuation =>
            {
                if (lastFailureRef.Value is { } captured && (continuation.IsFaulted || continuation.IsCanceled))
                {
                    return Task.FromException<TOutput>(captured);
                }

                return continuation;
            }, TaskContinuationOptions.ExecuteSynchronously)
            .Unwrap();
    }

    /// <summary>
    /// The inner loop that subscribes, does a pre-flight check, then reads events from
    /// the channel and retries the assertion on each trigger.
    /// </summary>
    private static async Task<TOutput> WaitForAssertionAsyncLoop<TGrain, TOutput>(
        IGrainCallCollectorProvider provider,
        TGrain grain,
        Predicate<IIncomingGrainCallContext> trigger,
        Func<TGrain, Task<TOutput>> assertion,
        StrongBox<Exception?> lastFailureRef,
        CancellationToken cancellationToken)
        where TGrain : IGrain
    {
        using var subscription = provider.CallCollector.Subscribe(out var reader, filter: trigger);

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (!Debugger.IsAttached)
        {
            linkedCts.CancelAfter(provider.WaitForAssertionAsyncTimeout);
        }

        var effectiveToken = linkedCts.Token;

        // Pre-flight: the assertion may already be true.
        using (RequestContextScope.ForAssertion())
        {
            try
            {
                return await assertion(grain).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastFailureRef.Value = ex;
            }
        }

        try
        {
            await foreach (var _ in reader.ReadAllAsync(effectiveToken).ConfigureAwait(false))
            {
                using (RequestContextScope.ForAssertion())
                {
                    try
                    {
                        return await assertion(grain).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        lastFailureRef.Value = ex;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (lastFailureRef.Value is not null)
        {
            // Fall through — the outer continuation rethrows the captured assertion exception.
        }

        if (lastFailureRef.Value is { } captured)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }

        throw new InvalidOperationException(
            "WaitForAssertionAsync exhausted its trigger stream without ever evaluating the assertion. " +
            "This indicates a bug in the helper or that the filter was disposed unexpectedly.");
    }
}