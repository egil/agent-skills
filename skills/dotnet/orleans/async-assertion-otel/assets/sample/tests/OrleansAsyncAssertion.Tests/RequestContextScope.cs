namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Creates a scoped RequestContext entry that is restored on dispose.
/// </summary>
public sealed class RequestContextScope : IDisposable
{
    /// <summary>
    /// Holds the request context key for this scope.
    /// </summary>
    private readonly string key;

    /// <summary>
    /// Stores the previous value for the key, if present.
    /// </summary>
    private readonly object? previousValue;

    /// <summary>
    /// Indicates whether a previous value existed for the key.
    /// </summary>
    private readonly bool hadPreviousValue;

    /// <summary>
    /// Initializes a new scope for the specified key and value.
    /// </summary>
    /// <param name="key">The request context key.</param>
    /// <param name="value">The request context value.</param>
    private RequestContextScope(string key, object value)
    {
        this.key = key;
        previousValue = RequestContext.Get(key);
        hadPreviousValue = previousValue is not null;
        RequestContext.Set(key, value);
    }

    /// <summary>
    /// Creates a scope with the provided key and value.
    /// </summary>
    /// <param name="key">The request context key.</param>
    /// <param name="value">The request context value.</param>
    /// <returns>A scope that restores the previous context on dispose.</returns>
    public static RequestContextScope With(string key, object value)
    {
        return new RequestContextScope(key, value);
    }

    /// <summary>
    /// Creates a scope that marks test assertions to avoid self-triggered spans.
    /// </summary>
    /// <returns>A scope with the assertion marker set.</returns>
    public static RequestContextScope ForAssertion()
    {
        return new RequestContextScope("test-assertion", true);
    }

    /// <summary>
    /// Restores the previous request context value.
    /// </summary>
    public void Dispose()
    {
        if (hadPreviousValue)
        {
            RequestContext.Set(key, previousValue!);
            return;
        }

        RequestContext.Remove(key);
    }
}
