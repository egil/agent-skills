using Xunit;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Unit tests for <see cref="RequestContextScope"/> covering assertion and test id scopes,
/// previous value restoration, and null argument handling.
/// </summary>
[Collection(nameof(RequestContextScopeTests))]
[CollectionDefinition(nameof(RequestContextScopeTests), DisableParallelization = true)]
public sealed class RequestContextScopeTests
{
    /// <summary>ForAssertion sets marker inside scope and removes it on dispose when unset before.</summary>
    [Fact]
    public void ForAssertion_sets_marker_inside_scope_and_removes_it_on_dispose_when_unset_before()
    {
        RequestContext.Remove(RequestContextScope.AssertionKey);

        using (RequestContextScope.ForAssertion())
        {
            Assert.Equal(true, RequestContext.Get(RequestContextScope.AssertionKey));
        }

        Assert.Null(RequestContext.Get(RequestContextScope.AssertionKey));
    }

    /// <summary>ForAssertion restores previous value on dispose.</summary>
    [Fact]
    public void ForAssertion_restores_previous_value_on_dispose()
    {
        RequestContext.Set(RequestContextScope.AssertionKey, "previous");
        try
        {
            using (RequestContextScope.ForAssertion())
            {
                Assert.Equal(true, RequestContext.Get(RequestContextScope.AssertionKey));
            }

            Assert.Equal("previous", RequestContext.Get(RequestContextScope.AssertionKey));
        }
        finally
        {
            RequestContext.Remove(RequestContextScope.AssertionKey);
        }
    }

    /// <summary>ForTest sets test id inside scope and removes it on dispose when unset before.</summary>
    [Fact]
    public void ForTest_sets_test_id_inside_scope_and_removes_it_on_dispose_when_unset_before()
    {
        RequestContext.Remove(RequestContextScope.TestIdKey);

        using (RequestContextScope.ForTest("test-123"))
        {
            Assert.Equal("test-123", RequestContext.Get(RequestContextScope.TestIdKey));
        }

        Assert.Null(RequestContext.Get(RequestContextScope.TestIdKey));
    }

    /// <summary>ForTest restores previous value on dispose.</summary>
    [Fact]
    public void ForTest_restores_previous_value_on_dispose()
    {
        RequestContext.Set(RequestContextScope.TestIdKey, "outer");
        try
        {
            using (RequestContextScope.ForTest("inner"))
            {
                Assert.Equal("inner", RequestContext.Get(RequestContextScope.TestIdKey));
            }

            Assert.Equal("outer", RequestContext.Get(RequestContextScope.TestIdKey));
        }
        finally
        {
            RequestContext.Remove(RequestContextScope.TestIdKey);
        }
    }

    /// <summary>ForTest throws on null test id.</summary>
    [Fact]
    public void ForTest_throws_on_null_test_id()
    {
        Assert.Throws<ArgumentNullException>(() => RequestContextScope.ForTest(null!));
    }
}
