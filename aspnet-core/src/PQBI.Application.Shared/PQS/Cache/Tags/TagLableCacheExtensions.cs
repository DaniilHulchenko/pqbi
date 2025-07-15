using Abp.Runtime.Caching;
using System.ComponentModel;
using System.Linq;


namespace PQBI.PQS.Cache.Tags;

public static class TagLableCacheExtensions
{
    public static ITypedCache<string, TagWithComponentCacheItem> GetTagWithComponentCache(this ICacheManager cacheManager)
    {
        return cacheManager.GetCache<string, TagWithComponentCacheItem>(TagWithComponentCacheItem.CacheName);
    }

    public static TagWithComponentCacheItem GetOrDefault(this ICacheManager cacheManager)
    {
        var ptr = cacheManager.GetTagWithComponentCache().GetOrDefault(TagWithComponentCacheItem.CacheName);
        return ptr;
    }
}
