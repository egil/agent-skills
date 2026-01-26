namespace OrleansServiceObserver.Contracts;

/// <summary>
/// Centralized constants keep grain keys stable across silos, services, and tests.
/// </summary>
public static class DataGrainConstants
{
    /// <summary>
    /// A fixed key gives every per-silo service a shared grain to subscribe to.
    /// </summary>
    public const string GrainKey = "global-cache";
}