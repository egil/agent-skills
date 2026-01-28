using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using OrleansAsyncAssertion.Grains;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Builds and tears down an Orleans test cluster for each test case.
/// </summary>
public sealed class ClusterFixture
{
    /// <summary>
    /// Provides a default timeout for async assertions.
    /// </summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Holds the request context scope used to tag calls for this test.
    /// </summary>
    private RequestContextScope? testScope;

    /// <summary>
    /// Gets the deployed test cluster instance.
    /// </summary>
    public TestCluster Cluster { get; private set; } = null!;

    /// <summary>
    /// Gets the trace collector used by this fixture.
    /// </summary>
    public TestTraceCollector TraceCollector { get; private set; } = null!;

    /// <summary>
    /// Gets the grain factory for the cluster.
    /// </summary>
    public IGrainFactory GrainFactory => Cluster.GrainFactory;

    /// <summary>
    /// Creates and deploys the test cluster.
    /// </summary>
    /// <returns>A fixture instance with a running cluster.</returns>
    public async ValueTask<ClusterFixture> InitializeAsync()
    {
        var builder = new TestClusterBuilder();
        builder.Options.InitialSilosCount = 1;
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        Cluster = builder.Build();
        await Cluster.DeployAsync();
        TraceCollector = new TestTraceCollector(TestTraceSources.All);
        var testId = TestIdProvider.ResolveTestId(fallback: null);
        if (!string.IsNullOrWhiteSpace(testId))
        {
            testScope = RequestContextScope.With("test-id", testId);
        }

        return this;
    }

    /// <summary>
    /// Stops all silos and disposes the cluster.
    /// </summary>
    /// <returns>A task that completes once cleanup is finished.</returns>
    public async ValueTask DisposeAsync()
    {
        testScope?.Dispose();
        await TraceCollector.DisposeAsync();
        await Cluster.StopAllSilosAsync();
        await Cluster.DisposeAsync();
    }

    /// <summary>
    /// Waits for an assertion to pass, triggering on any observed activity.
    /// </summary>
    /// <param name="assertion">The assertion to execute.</param>
    /// <param name="timeout">An optional timeout override.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="callerMemberName">The caller member name.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    public Task WaitForAssertionAsync(
        Func<Task> assertion,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string? callerMemberName = null)
    {
        var cursor = new ActivityCursor(0);
        var wrappedAssertion = WrapAssertion(assertion, callerMemberName);
        return ActivityWaiter.WaitForAssertionAsync(
            TraceCollector,
            cursor,
            ActivityFilters.AnyActivity(),
            wrappedAssertion,
            timeout ?? DefaultTimeout,
            cancellationToken);
    }

    /// <summary>
    /// Waits for an assertion to pass, triggering on activity related to the specified grain.
    /// </summary>
    /// <typeparam name="TGrain">The grain type.</typeparam>
    /// <param name="grain">The grain instance used to scope the trigger.</param>
    /// <param name="assertion">The assertion to execute.</param>
    /// <param name="timeout">An optional timeout override.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="callerMemberName">The caller member name.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    public Task WaitForAssertionAsync<TGrain>(
        TGrain grain,
        Func<TGrain, Task> assertion,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string? callerMemberName = null)
        where TGrain : Orleans.IGrain
    {
        var cursor = new ActivityCursor(0);
        var grainId = Orleans.GrainExtensions.GetGrainId(grain).ToString();
        var wrappedAssertion = WrapAssertion(() => assertion(grain), callerMemberName);
        return ActivityWaiter.WaitForAssertionAsync(
            TraceCollector,
            cursor,
            ActivityFilters.OrleansGrain(grainId),
            wrappedAssertion,
            timeout ?? DefaultTimeout,
            cancellationToken);
    }

    /// <summary>
    /// Waits for an assertion to pass, triggering on activity for a specific grain method.
    /// </summary>
    /// <typeparam name="TGrain">The grain type.</typeparam>
    /// <param name="grain">The grain instance used to scope the trigger.</param>
    /// <param name="assertion">The assertion to execute.</param>
    /// <param name="trigger">An expression that identifies the triggering method.</param>
    /// <param name="timeout">An optional timeout override.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="callerMemberName">The caller member name.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    public Task WaitForAssertionAsync<TGrain>(
        TGrain grain,
        Func<TGrain, Task> assertion,
        Expression<Func<TGrain, Task>> trigger,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string? callerMemberName = null)
        where TGrain : Orleans.IGrain
    {
        var cursor = new ActivityCursor(0);
        var grainId = Orleans.GrainExtensions.GetGrainId(grain).ToString();
        var methodName = GetMethodName(trigger);
        var wrappedAssertion = WrapAssertion(() => assertion(grain), callerMemberName);
        return ActivityWaiter.WaitForAssertionAsync(
            TraceCollector,
            cursor,
            ActivityFilters.OrleansGrainMethod(grainId, methodName),
            wrappedAssertion,
            timeout ?? DefaultTimeout,
            cancellationToken);
    }

    /// <summary>
    /// Waits for an assertion to pass, triggering on activity for a specific grain method.
    /// </summary>
    /// <typeparam name="TGrain">The grain type.</typeparam>
    /// <param name="assertion">The assertion to execute.</param>
    /// <param name="trigger">An expression that identifies the triggering method.</param>
    /// <param name="timeout">An optional timeout override.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="callerMemberName">The caller member name.</param>
    /// <returns>A task that completes when the assertion passes.</returns>
    public Task WaitForAssertionAsync<TGrain>(
        Func<Task> assertion,
        Expression<Func<TGrain, Task>> trigger,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string? callerMemberName = null)
    {
        var cursor = new ActivityCursor(0);
        var methodName = GetMethodName(trigger);
        var grainTypeName = typeof(TGrain).FullName ?? typeof(TGrain).Name;
        var wrappedAssertion = WrapAssertion(assertion, callerMemberName);
        return ActivityWaiter.WaitForAssertionAsync(
            TraceCollector,
            cursor,
            ActivityFilters.OrleansMethod(grainTypeName, methodName),
            wrappedAssertion,
            timeout ?? DefaultTimeout,
            cancellationToken);
    }

    /// <summary>
    /// Wraps an assertion with request context metadata for tracing.
    /// </summary>
    /// <param name="assertion">The assertion to execute.</param>
    /// <param name="callerMemberName">The caller member name.</param>
    /// <returns>The wrapped assertion.</returns>
    private static Func<Task> WrapAssertion(Func<Task> assertion, string? callerMemberName)
    {
        var testId = TestIdProvider.ResolveTestId(callerMemberName);
        return async () =>
        {
            using var assertionScope = RequestContextScope.ForAssertion();
            using var assertionTestScope = string.IsNullOrWhiteSpace(testId)
                ? null
                : RequestContextScope.With("test-id", testId);
            await assertion().ConfigureAwait(false);
        };
    }

    /// <summary>
    /// Extracts the method name from a grain call expression.
    /// </summary>
    /// <typeparam name="TGrain">The grain type.</typeparam>
    /// <param name="trigger">The expression to inspect.</param>
    /// <returns>The method name.</returns>
    private static string GetMethodName<TGrain>(Expression<Func<TGrain, Task>> trigger)
    {
        if (trigger.Body is MethodCallExpression methodCall)
        {
            return methodCall.Method.Name;
        }

        throw new ArgumentException("Trigger expression must be a method call.", nameof(trigger));
    }

    /// <summary>
    /// Configures per-silo services for the test cluster.
    /// </summary>
    private sealed class SiloConfigurator : ISiloConfigurator
    {
        /// <summary>
        /// Registers streams and tracing filters on each silo.
        /// </summary>
        /// <param name="siloBuilder">The silo builder to configure.</param>
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder.AddMemoryGrainStorageAsDefault();
            siloBuilder.AddMemoryGrainStorage("PubSubStore");
            siloBuilder.AddMemoryStreams(StreamConstants.StreamProviderName);
            siloBuilder.AddIncomingGrainCallFilter<TestTraceTaggingFilter>();
            siloBuilder.Services.AddSingleton<TestTraceTaggingFilter>();
        }
    }
}
