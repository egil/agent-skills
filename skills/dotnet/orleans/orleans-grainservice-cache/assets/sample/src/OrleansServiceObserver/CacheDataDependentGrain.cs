using OrleansServiceObserver.Contracts;

namespace OrleansServiceObserver.Grains;

public interface ICacheDataDependentGrain : IGrainWithStringKey
{
    Task<string?> GetValue();
}

/// <summary>
/// A grain that uses cached data from a per-silo grain service.
/// </summary>
public sealed class CacheDataDependentGrain(IDataCache dataCache) : Grain, ICacheDataDependentGrain
{
    public async Task<string?> GetValue()
    {
        var result = await dataCache.GetValue(DataGrainConstants.GrainKey);
        return result;
    }
}