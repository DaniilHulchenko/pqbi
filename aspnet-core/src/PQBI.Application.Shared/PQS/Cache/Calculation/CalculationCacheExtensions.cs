using Abp.Runtime.Caching;
using System;
using System.Threading.Tasks;

namespace PQBI.PQS.Cache.Calculation;

public static class CalculationCacheNames
{
    public const string Historical = "CalculationCache";
    public const string Realtime = "RealtimeCalculationCache";
}

public static class CalculationCacheItemExtensions
{

    public static async Task SetCalculationCacheAsync(this CalculationCacheItem item, ICacheManager cacheManager)
    {
        if (item.PQBIAxisData is null)
            throw new Exception($"{nameof(item.PQBIAxisData)} can not be null");

     
        var cache = cacheManager.GetCache<string, CalculationCacheItem>(CalculationCacheNames.Historical);
        await cache.SetAsync(item.CacheKey, item);
    }

    public static bool TryGetCalculationCache(this CalculationCacheItem item, ICacheManager cacheManager, out CalculationCacheItem value)
    {
        var cache = cacheManager.GetCache<string, CalculationCacheItem>(CalculationCacheNames.Historical);
        return cache.TryGetValue(item.CacheKey, out value);
    }


    public static ITypedCache<string, CalculationCacheItem> GetCalculationCache(this CalculationCacheItem calculationCacheItem, ICacheManager cacheManager)
    {
        var item = cacheManager.GetCache<string, CalculationCacheItem>(calculationCacheItem.CacheKey);
        return item;
    }
}


public static class CalculationCacheExtensions
{

    public static bool TryGetCalculationCache(this ICacheManager cacheManager, string key, out CalculationCacheItem value)
    {
        var cache = cacheManager.GetCache<string, CalculationCacheItem>(CalculationCacheNames.Historical);
        return cache.TryGetValue(key, out value);
    }

    //public static bool TryGetCalculationCache(this ICacheManager cacheManager, string key, out CalculationCacheItem CalculationCacheItem)
    //{
    //    var item = cacheManager.GetCache<string, CalculationCacheItem>(key);
    //    var result = item.TryGetValue(key, out CalculationCacheItem);
    //    return result;
    //}
}

