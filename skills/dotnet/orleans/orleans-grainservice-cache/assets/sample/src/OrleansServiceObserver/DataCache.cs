using System.Collections.Concurrent;

namespace OrleansServiceObserver.Grains;

public interface IDataCache
{
    Task<string?> GetValue(string grainKey);
}

public class DataCache(IGrainFactory grainFactory) : IDataCache
{
    private readonly ConcurrentDictionary<string, Task<string?>> cache = new(StringComparer.Ordinal);

    public Task<string?> GetValue(string grainKey)
        => cache.GetOrAdd(grainKey, async key =>
        {
            var grain = grainFactory.GetGrain<IDataGrain>(key);
            var value = await grain.GetValue();
            return value;
        });

    public void OnDataUpdated(string grainKey, string? value)
        => cache.AddOrUpdate(
            grainKey,
            _ => Task.FromResult(value),
            (_, _) => Task.FromResult(value));
}