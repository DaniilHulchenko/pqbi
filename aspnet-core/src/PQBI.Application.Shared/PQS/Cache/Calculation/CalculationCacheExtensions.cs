using Abp.Runtime.Caching;
using System;
using System.Threading.Tasks;

namespace PQBI.PQS.Cache.Calculation;

public static class CalculationCacheItemExtensions
{
    public static async Task SetCalculationCacheAsync(this CalculationCacheItem item, ICacheManager cacheManager)
    {
        if (item.PQBIAxisData is null)
        {
            throw new Exception($"{nameof(item.PQBIAxisData)} can not be null");
        }

        var cache = item.GetCalculationCache(cacheManager);
        await cache.SetAsync(item.CacheKey, item);
    }

    public static bool TryGetCalculationCache(this CalculationCacheItem item, ICacheManager cacheManager, out CalculationCacheItem CalculationCacheItem)
    {
        return cacheManager.TryGetCalculationCache(item.CacheKey, out CalculationCacheItem);
    }

    public static ITypedCache<string, CalculationCacheItem> GetCalculationCache(this CalculationCacheItem calculationCacheItem, ICacheManager cacheManager)
    {
        var item = cacheManager.GetCache<string, CalculationCacheItem>(calculationCacheItem.CacheKey);
        return item;
    }
}


public static class CalculationCacheExtensions
{
    public static bool TryGetCalculationCache(this ICacheManager cacheManager, string key, out CalculationCacheItem CalculationCacheItem)
    {
        var item = cacheManager.GetCache<string, CalculationCacheItem>(key);
        var result = item.TryGetValue(key, out CalculationCacheItem);
        return result;
    }
}

