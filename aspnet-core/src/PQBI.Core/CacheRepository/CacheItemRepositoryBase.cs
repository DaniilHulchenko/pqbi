using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PQBI.Infrastructure;
using PQBI.Infrastructure.Lockers;
using System;
using System.Threading.Tasks;

namespace PQBI.Caching
{

    public abstract class CacheItemRepositoryBase<TKey, TValue>
    {
        // Only one lock can be granted and a max of one lock
        //private readonly SemaphoreSlimAutoLocker _locker;

        protected readonly ILogger _logger;
        private readonly PqbiSafeEntityLockerSlim<IMemoryCache> _cacheWrapper;


        public CacheItemRepositoryBase(IMemoryCache cache, ILogger logger)
        {
            //_locker = new SemaphoreSlimAutoLocker(1, 1);
            _cacheWrapper = new PqbiSafeEntityLockerSlim<IMemoryCache>(cache);
            _logger = logger;
        }

        /// <summary>
        /// Sets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        public virtual TimeSpan? UnusedExpireTime => null;

        /// <summary>
        /// Sets an absolute expiration time, relative to now.
        /// </summary>
        public virtual TimeSpan? AbsoluteExpirationRelativeToNow => null;

        /// <summary>
        /// Callback will be fired after the cache entry is evicted from the cache
        /// </summary>
        public virtual PostEvictionDelegate PostEvictionDelegate => null;

        /// <summary>
        /// Setting the data by providing a key and a value.
        /// Create/Update will be be done with this method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public virtual async Task SetMemCacheItemAsync(TKey key, TValue data)
        {
            var options = new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = AbsoluteExpirationRelativeToNow,
                SlidingExpiration = UnusedExpireTime,
            };

            if (PostEvictionDelegate != null)
            {
                options.RegisterPostEvictionCallback(PostEvictionDelegate);
            }

            await _cacheWrapper.DoLockAsync(cache => cache.Set(key, data, options));
        }


        /// <summary>
        /// Checking in merely in cache.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool TryGetUserCacheItem(TKey key, out TValue value)
        {
            var result = false;
            var val = default(TValue);

            _cacheWrapper.Do(mem =>
            {
                if (mem.TryGetValue(key, out val))
                {
                    result = true;
                }
            });

            value = val;
            return result;
        }

        public virtual async Task<bool> RemoveCacheItemAsync(TKey key)
        {
            var result = true;
            try
            {
                await _cacheWrapper.DoLockAsync(mem => mem.Remove(key));
            }
            catch (Exception ex)
            {
                _logger.LogError($"During deletion of item {key} an error occured {ex.ToString()}");
                result = false;
            }

            return result;
        }

        public async Task KeepAliveInCacheAsync(TKey userId)
        {
            TryGetUserCacheItem(userId, out _);
        }
    }
}
