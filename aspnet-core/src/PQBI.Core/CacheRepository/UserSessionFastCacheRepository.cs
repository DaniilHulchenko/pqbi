using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PQBI.Caching;
using PQBI.Logs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PQBI.CacheRepository
{
    internal class UserSessionFastCacheRepository : CacheItemRepositoryBase<long, UserCacheItemDto>
    {

        /// <summary>
        /// Mirror for <see cref="IMemoryCache"/>.
        /// </summary>
        private readonly ConcurrentDictionary<long, UserCacheItemDto> _usersCacheDictionary;


        public UserSessionFastCacheRepository(IMemoryCache cache, ILogger logger, PQSUserCacheConfig config)
            : base(cache, logger)
        {
            _usersCacheDictionary = new ConcurrentDictionary<long, UserCacheItemDto>();
            Config = config;
        }

        public override TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromHours(24);
        public override TimeSpan? UnusedExpireTime => TimeSpan.FromMinutes(Config.UnusedExpireTimeInMinute); // TimeSpan.FromMinutes(10);

        /// <summary>
        /// Indication for callback when this cache dies.
        /// </summary>
        public override PostEvictionDelegate PostEvictionDelegate => PostEvictionCallback;


        public PQSUserCacheConfig Config { get; }


        public long[] PeekUserIds => _usersCacheDictionary.Keys.ToArray();


        public IEnumerable<UserTenant> PeekUserInfos()
        {
            var list = new List<UserTenant>();

            foreach (var userId in PeekUserIds)
            {
                if (_usersCacheDictionary.TryGetValue(userId, out var userCacheItem))
                {
                    list.Add(new UserTenant(userId, userCacheItem.TenantId, userCacheItem.PQSSession));
                }
            }

            return list;
        }

        public bool TryPeekCacheItem(long userId, out UserCacheItemDto userCacheItemDto)
        {
            userCacheItemDto = null;
            var result = false;
            if (_usersCacheDictionary.TryGetValue(userId, out var userDto))
            {
                userCacheItemDto = userDto;
                result = true;
            }

            return result;
        }

        public override async Task SetMemCacheItemAsync(long userId, UserCacheItemDto userCacheData)
        {
            var serialized = JsonConvert.SerializeObject(userCacheData);
            _logger.LogWarning($"UserSessionFastCacheRepository | SetMemCacheItemAsync userCacheData= {serialized}");

            await base.SetMemCacheItemAsync(userId, userCacheData);
            _usersCacheDictionary[userId] = userCacheData.Clone();
        }

        /// <summary>
        /// This callback would be invoked when UserCacheItemDto dies!
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="reason"></param>
        /// <param name="state"></param>
        private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            if (key is long userId)
            {
                if (reason == EvictionReason.Replaced)
                {
                    _logger.LogWarning($"{nameof(UserSessionFastCacheRepository)} | {nameof(PostEvictionCallback)} userId= {userId}  reason = replaced ");
                    return;
                }

                Task.Run(async () =>
                {
                    _logger.LogWarning($"{nameof(UserSessionFastCacheRepository)} | {nameof(PostEvictionCallback)} | userId= {userId} reason = {reason} ");
                    if (TryPeekCacheItem(userId, out var uerCacheDto))
                    {
                        _logger.LogSession(uerCacheDto.TenantId, userId, $"Cache {reason}");
                    }
                    await RemoveCacheItemAsync(userId);
                });
            }
            else
            {
                _logger.LogWarning($"None user id has changed the cache.");
            }
        }


        public override async Task<bool> RemoveCacheItemAsync(long userId)
        {
            _usersCacheDictionary.TryRemove(userId, out var userCacheItemDto);
            await base.RemoveCacheItemAsync(userId);

            return true;
        }
    }
}
