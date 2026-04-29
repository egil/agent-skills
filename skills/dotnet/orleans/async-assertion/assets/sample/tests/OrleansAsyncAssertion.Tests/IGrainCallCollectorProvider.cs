namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Implemented by test fixtures that register a <see cref="GrainCallCollectionFilter"/>
/// as an <see cref="IIncomingGrainCallFilter"/> in their silo. Exposes the collector
/// and the per-fixture timeout so that the <c>WaitForAssertionAsync</c> extension methods
/// can operate on any conforming fixture.
/// </summary>
public interface IGrainCallCollectorProvider : IGrainFactory
{
    /// <summary>
    /// Safety-net timeout applied to every <c>WaitForAssertionAsync</c> invocation
    /// (linked with <c>TestContext.Current.CancellationToken</c>). Bypassed when
    /// <see cref="System.Diagnostics.Debugger.IsAttached"/> is <see langword="true"/>.
    /// Override globally via the <c>WAIT_FOR_ASSERTION_TIMEOUT_SECONDS</c> environment variable.
    /// </summary>
    static readonly TimeSpan DefaultWaitForAssertionAsyncTimeout =
        int.TryParse(Environment.GetEnvironmentVariable("WAIT_FOR_ASSERTION_TIMEOUT_SECONDS"), out var envSeconds) && envSeconds > 0
            ? TimeSpan.FromSeconds(envSeconds)
            : TimeSpan.FromSeconds(5);

    /// <summary>The grain call collector registered in the test silo.</summary>
    GrainCallCollectionFilter CallCollector { get; }

    /// <summary>
    /// Per-fixture safety-net timeout for <c>WaitForAssertionAsync</c>. Defaults to
    /// <see cref="DefaultWaitForAssertionAsyncTimeout"/>. Lower this in helper unit
    /// tests that intentionally drive the timeout path.
    /// </summary>
    TimeSpan WaitForAssertionAsyncTimeout { get; set; }
}
