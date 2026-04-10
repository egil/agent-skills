using System.Reflection;
using Xunit;
using Xunit.v3;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// A <see cref="BeforeAfterTestAttribute"/> that registers each test with the
/// <see cref="AsyncClusterFixture"/>'s grain call collector before the test body runs
/// and unregisters it afterwards. This enables marker-based log pruning and provides
/// <c>WaitForAssertionAsync</c> with each test's start position.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to a test class (or use <see cref="AsyncAssertionTestBase"/>
/// which applies it automatically). The attribute looks up the <see cref="AsyncClusterFixture"/>
/// from <see cref="TestContext.Current"/>.<see cref="TestContext.KeyValueStorage"/> — the fixture
/// must have been registered via <see cref="AsyncClusterFixture.RegisterForCurrentTest"/>
/// in the test class constructor.
/// </para>
/// <para>
/// xUnit v3 discovers <see cref="IBeforeAfterTestAttribute"/> on test classes (and assemblies,
/// collection definitions, and test methods), but NOT on fixture types.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class CollectGrainCallsAttribute : BeforeAfterTestAttribute
{
    /// <summary>
    /// The key used to store/retrieve the <see cref="AsyncClusterFixture"/> in
    /// <see cref="TestContext.KeyValueStorage"/>.
    /// </summary>
    internal const string FixtureKey = nameof(AsyncClusterFixture);

    /// <summary>
    /// The key used to store/retrieve the grain call log start position in
    /// <see cref="TestContext.KeyValueStorage"/>.
    /// </summary>
    internal const string StartPositionKey = "GrainCallLog.StartPosition";

    /// <summary>
    /// Registers the test with the grain call collector, recording the current log position.
    /// </summary>
    /// <param name="methodUnderTest">The test method about to execute.</param>
    /// <param name="test">The xUnit test instance containing the unique test ID.</param>
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current?.KeyValueStorage.TryGetValue(FixtureKey, out var obj) == true
            && obj is AsyncClusterFixture fixture)
        {
            var position = fixture.RegisterTestStart(test.UniqueID);
            TestContext.Current.KeyValueStorage[StartPositionKey] = position;
        }
    }

    /// <summary>
    /// Unregisters the test from the grain call collector, triggering log pruning.
    /// </summary>
    /// <param name="methodUnderTest">The test method that finished executing.</param>
    /// <param name="test">The xUnit test instance containing the unique test ID.</param>
    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (TestContext.Current?.KeyValueStorage.TryGetValue(FixtureKey, out var obj) == true
            && obj is AsyncClusterFixture fixture)
        {
            fixture.RegisterTestEnd(test.UniqueID);
        }
    }
}
