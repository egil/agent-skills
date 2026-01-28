using System.Diagnostics;

namespace OrleansAsyncAssertion.Tests;

/// <summary>
/// Provides helpers for filtering activities emitted by Orleans and custom sources.
/// </summary>
public static class ActivityFilters
{
    /// <summary>
    /// Returns a predicate that matches Orleans grain calls for a specific test ID.
    /// </summary>
    /// <param name="testId">The test identifier to match.</param>
    /// <param name="grainType">The full name of the grain interface.</param>
    /// <param name="methodName">The grain method name.</param>
    /// <returns>A predicate that matches the desired activity.</returns>
    public static Func<Activity, bool> OrleansMethod(string testId, string grainType, string methodName)
    {
        return activity =>
        {
            if (IsAssertionActivity(activity))
            {
                return false;
            }

            if (!HasTagValue(activity, "test.id", testId))
            {
                return false;
            }

            return HasTagValue(activity, "orleans.grain", grainType)
                && HasTagValue(activity, "orleans.method", methodName);
        };
    }

    /// <summary>
    /// Returns a predicate that matches Orleans grain calls for a specific interface and method name.
    /// </summary>
    /// <param name="grainType">The full name of the grain interface.</param>
    /// <param name="methodName">The grain method name.</param>
    /// <returns>A predicate that matches the desired activity.</returns>
    public static Func<Activity, bool> OrleansMethod(string grainType, string methodName)
    {
        return activity =>
        {
            if (IsAssertionActivity(activity))
            {
                return false;
            }

            return HasTagValue(activity, "orleans.grain", grainType)
                && HasTagValue(activity, "orleans.method", methodName);
        };
    }

    /// <summary>
    /// Returns a predicate that matches Orleans grain calls for a specific grain ID.
    /// </summary>
    /// <param name="grainId">The grain ID to match.</param>
    /// <returns>A predicate that matches the desired activity.</returns>
    public static Func<Activity, bool> OrleansGrain(string grainId)
    {
        return activity =>
        {
            if (IsAssertionActivity(activity))
            {
                return false;
            }

            return HasTagValue(activity, "orleans.grain_id", grainId);
        };
    }

    /// <summary>
    /// Returns a predicate that matches Orleans grain calls for a specific grain ID and method name.
    /// </summary>
    /// <param name="grainId">The grain ID to match.</param>
    /// <param name="methodName">The grain method name.</param>
    /// <returns>A predicate that matches the desired activity.</returns>
    public static Func<Activity, bool> OrleansGrainMethod(string grainId, string methodName)
    {
        return activity =>
        {
            if (IsAssertionActivity(activity))
            {
                return false;
            }

            return HasTagValue(activity, "orleans.grain_id", grainId)
                && HasTagValue(activity, "orleans.method", methodName);
        };
    }

    /// <summary>
    /// Returns a predicate that matches stream delivery activities for a specific test ID and stream ID.
    /// </summary>
    /// <param name="testId">The test identifier to match.</param>
    /// <param name="streamId">The stream identifier to match.</param>
    /// <returns>A predicate that matches the desired activity.</returns>
    public static Func<Activity, bool> StreamDelivery(string testId, Guid streamId)
    {
        var streamIdTag = streamId.ToString("N");
        return activity =>
        {
            if (IsAssertionActivity(activity))
            {
                return false;
            }

            if (!HasTagValue(activity, "test.id", testId))
            {
                return false;
            }

            return HasTagValue(activity, "stream.id", streamIdTag)
                && HasTagValue(activity, "stream.namespace", OrleansAsyncAssertion.Grains.StreamConstants.StreamNamespace);
        };
    }

    /// <summary>
    /// Returns a predicate that matches stream delivery activities for a specific stream ID.
    /// </summary>
    /// <param name="streamId">The stream identifier to match.</param>
    /// <returns>A predicate that matches the desired activity.</returns>
    public static Func<Activity, bool> StreamDelivery(Guid streamId)
    {
        var streamIdTag = streamId.ToString("N");
        return activity =>
        {
            if (IsAssertionActivity(activity))
            {
                return false;
            }

            return HasTagValue(activity, "stream.id", streamIdTag)
                && HasTagValue(activity, "stream.namespace", OrleansAsyncAssertion.Grains.StreamConstants.StreamNamespace);
        };
    }

    /// <summary>
    /// Returns a predicate that matches any non-assertion activity.
    /// </summary>
    /// <returns>A predicate that matches all non-assertion activities.</returns>
    public static Func<Activity, bool> AnyActivity()
    {
        return activity => !IsAssertionActivity(activity);
    }

    /// <summary>
    /// Determines whether the activity was emitted while running a test assertion.
    /// </summary>
    /// <param name="activity">The activity to inspect.</param>
    /// <returns><c>true</c> if the activity should be ignored; otherwise <c>false</c>.</returns>
    private static bool IsAssertionActivity(Activity activity)
    {
        return activity.GetTagItem("test.assertion") is true;
    }

    /// <summary>
    /// Checks whether an activity contains a specific tag value.
    /// </summary>
    /// <param name="activity">The activity to inspect.</param>
    /// <param name="key">The tag key.</param>
    /// <param name="value">The expected value.</param>
    /// <returns><c>true</c> when the tag is present and matches; otherwise <c>false</c>.</returns>
    private static bool HasTagValue(Activity activity, string key, string value)
    {
        return activity.GetTagItem(key) is string actual && string.Equals(actual, value, StringComparison.Ordinal);
    }
}
