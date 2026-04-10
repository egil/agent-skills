using Xunit;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Optional abstract base class for test classes that use <see cref="AsyncClusterFixture"/>
/// with grain call collection. Applies <see cref="CollectGrainCallsAttribute"/> (inherited by
/// derived classes) and calls <see cref="AsyncClusterFixture.RegisterForCurrentTest"/> in the
/// constructor to wire up the fixture for the current test.
/// </summary>
/// <remarks>
/// <para>
/// Derived classes should add <see cref="IClassFixture{TFixture}"/> or use a collection fixture
/// depending on the desired fixture lifetime:
/// </para>
/// <code>
/// // Per-class fixture:
/// public sealed class MyTests(AsyncClusterFixture fixture)
///     : AsyncAssertionTestBase(fixture), IClassFixture&lt;AsyncClusterFixture&gt; { }
///
/// // Collection fixture (shared across classes):
/// [Collection("AsyncCluster")]
/// public sealed class MyTests(AsyncClusterFixture fixture)
///     : AsyncAssertionTestBase(fixture) { }
/// </code>
/// </remarks>
[CollectGrainCalls]
public abstract class AsyncAssertionTestBase
{
    /// <summary>
    /// Gets the <see cref="AsyncClusterFixture"/> provided to this test class.
    /// </summary>
    protected AsyncClusterFixture Fixture { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncAssertionTestBase"/> and registers the
    /// fixture for the current test's grain call collection.
    /// </summary>
    /// <param name="fixture">The test cluster fixture.</param>
    protected AsyncAssertionTestBase(AsyncClusterFixture fixture)
    {
        Fixture = fixture;
        fixture.RegisterForCurrentTest();
    }
}
