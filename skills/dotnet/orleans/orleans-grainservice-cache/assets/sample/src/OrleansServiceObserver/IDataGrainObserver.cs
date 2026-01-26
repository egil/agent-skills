using Orleans.Concurrency;

namespace OrleansServiceObserver.Grains;

/// <summary>
/// Represents a grain observer interface for data grain update notifications.
/// </summary>
public interface IDataGrainObserver : IGrainObserver
{
    /// <summary>
    /// Called by the authoritative data grain when its value changes.
    /// </summary>
    /// <remarks>
    /// This method is marked as AlwaysInterleave to allow notifications to be processed while
    /// the grain service is subscribing.
    /// </remarks>
    [AlwaysInterleave]
    Task OnDataUpdated(string grainKey, string? value);
}