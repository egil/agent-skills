using System.Reflection;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Resolves a stable test identifier for tagging trace activities.
/// </summary>
public static class TestIdProvider
{
    /// <summary>
    /// Resolves a test ID using the xUnit test context, falling back to the provided value.
    /// </summary>
    /// <param name="fallback">The fallback value, typically from CallerMemberName.</param>
    /// <returns>A test identifier, or <c>null</c> if none is available.</returns>
    public static string? ResolveTestId(string? fallback)
    {
        var fromContext = TryGetXunitTestId();
        if (!string.IsNullOrWhiteSpace(fromContext))
        {
            return fromContext;
        }

        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }

    /// <summary>
    /// Attempts to extract the test identifier from xUnit's TestContext.
    /// </summary>
    /// <returns>The test identifier, or <c>null</c> if unavailable.</returns>
    private static string? TryGetXunitTestId()
    {
        var testContextType = Type.GetType("Xunit.TestContext, xunit.v3.core");
        if (testContextType is null)
        {
            return null;
        }

        var current = testContextType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(null);
        if (current is null)
        {
            return null;
        }

        var testObject = current.GetType().GetProperty("Test", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(current);
        if (testObject is null)
        {
            return null;
        }

        var uniqueId = GetStringProperty(testObject, "UniqueID");
        if (!string.IsNullOrWhiteSpace(uniqueId))
        {
            return uniqueId;
        }

        return GetStringProperty(testObject, "DisplayName");
    }

    /// <summary>
    /// Reads a string property via reflection.
    /// </summary>
    /// <param name="target">The object to inspect.</param>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The string value, or <c>null</c>.</returns>
    private static string? GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(target);
        return value as string;
    }
}
