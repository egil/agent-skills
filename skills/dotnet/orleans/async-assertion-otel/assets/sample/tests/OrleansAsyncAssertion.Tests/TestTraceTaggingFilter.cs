using System.Diagnostics;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Adds test metadata to incoming grain calls so traces can be correlated to tests.
/// </summary>
public sealed class TestTraceTaggingFilter : IIncomingGrainCallFilter
{
    /// <summary>
    /// Orleans uses this activity source for application-level spans.
    /// </summary>
    private static readonly ActivitySource ActivitySource = new("Microsoft.Orleans.Application");

    /// <summary>
    /// Adds test tags and Orleans metadata to the current activity.
    /// </summary>
    /// <param name="context">The incoming grain call context.</param>
    /// <returns>A task that completes when the call has been invoked.</returns>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var activity = Activity.Current;
        var created = false;
        if (activity is null)
        {
            activity = ActivitySource.StartActivity("GrainCall", ActivityKind.Server);
            created = activity is not null;
        }

        if (activity is not null)
        {
            if (RequestContext.Get("test-id") is string testId)
            {
                activity.SetTag("test.id", testId);
            }

            if (RequestContext.Get("test-assertion") is true)
            {
                activity.SetTag("test.assertion", true);
            }

            var interfaceMethod = context.InterfaceMethod;
            var implementationMethod = context.ImplementationMethod;
            activity.SetTag("orleans.method", interfaceMethod?.Name ?? implementationMethod?.Name);
            activity.SetTag(
                "orleans.grain",
                interfaceMethod?.DeclaringType?.FullName ?? implementationMethod?.DeclaringType?.FullName);
            activity.SetTag("orleans.grain_id", context.TargetContext.GrainId.ToString());
        }

        if (created && activity is not null)
        {
            using (activity)
            {
                await context.Invoke();
            }

            return;
        }

        await context.Invoke();
    }
}
