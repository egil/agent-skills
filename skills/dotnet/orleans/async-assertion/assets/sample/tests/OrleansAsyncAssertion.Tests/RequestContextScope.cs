namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Helpers for scoping <see cref="RequestContext"/> entries that
/// <see cref="GrainCallCollectionFilter"/> understands.
/// </summary>
/// <remarks>
/// Each helper captures the previous value of the affected key on construction and restores it
/// on dispose. If the key was unset before the scope began, dispose removes it; otherwise dispose
/// puts the previous value back.
/// </remarks>
public static class RequestContextScope
{
    /// <summary>The <see cref="RequestContext"/> key used to mark assertion-machinery calls.</summary>
    public const string AssertionKey = "test-assertion";

    /// <summary>The <see cref="RequestContext"/> key used to attribute calls to a test id.</summary>
    public const string TestIdKey = "test.id";

    /// <summary>
    /// Marks the wrapped scope as test-assertion machinery so the collection filter ignores
    /// any grain calls made from inside it.
    /// </summary>
    /// <returns>A disposable that restores the previous <see cref="AssertionKey"/> value on dispose.</returns>
    public static IDisposable ForAssertion() => new Scope(AssertionKey, true);

    /// <summary>
    /// Pins a test correlation id into <see cref="RequestContext"/> so the filter can attribute
    /// trigger events to the originating test.
    /// </summary>
    /// <param name="testId">The test correlation id; must be non-null.</param>
    /// <returns>A disposable that restores the previous <see cref="TestIdKey"/> value on dispose.</returns>
    public static IDisposable ForTest(string testId)
    {
        ArgumentNullException.ThrowIfNull(testId);
        return new Scope(TestIdKey, testId);
    }

    /// <summary>
    /// Internal scope that captures and restores a single <see cref="RequestContext"/> key.
    /// </summary>
    private sealed class Scope : IDisposable
    {
        /// <summary>The request context key managed by this scope.</summary>
        private readonly string key;

        /// <summary>The previous value of the key before this scope was created.</summary>
        private readonly object? previousValue;

        /// <summary>Whether a previous value existed before this scope was created.</summary>
        private readonly bool hadPreviousValue;

        /// <summary>Tracks whether dispose has been called (0 = not disposed, 1 = disposed).</summary>
        private int disposed;

        /// <summary>
        /// Initializes a new scope, saving the current value and setting the new one.
        /// </summary>
        /// <param name="key">The request context key.</param>
        /// <param name="value">The value to set for the duration of this scope.</param>
        public Scope(string key, object value)
        {
            this.key = key;
            previousValue = RequestContext.Get(key);
            hadPreviousValue = previousValue is not null;
            RequestContext.Set(key, value);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            if (hadPreviousValue)
            {
                RequestContext.Set(key, previousValue!);
            }
            else
            {
                RequestContext.Remove(key);
            }
        }
    }
}