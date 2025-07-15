using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PQBI.Authorization.Users;
using PQBI.CacheRepository;
using PQBI.Infrastructure;
using PQBI.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PQBI.Caching
{
    public class UserCacheItemDto
    {
        /// <summary>
        /// Every user has its uniqe session.
        /// </summary>
        public string PQSSession { get; }
        public int TenantId { get; }

        public UserCacheItemDto(int tenantId, string pQSSession)
        {
            PQSSession = pQSSession;
            TenantId = tenantId;
        }

        public UserCacheItemDto Clone()
        {
            return (UserCacheItemDto)MemberwiseClone();
        }
    }

    public class UserTenant : UserCacheItemDto
    {
        public UserTenant(long userId, int tenantId, string session)
            : base(tenantId, session)
        {
            UserId = userId;
        }

        public long UserId { get; }
    }

    public interface IUserSessionCacheRepository 
    {
        Task<bool> SetCacheSessionAsync(long userId, int tenantId, string pQSSession);
        Task<string> GetCacheSessionAsync(long userId);
        IEnumerable<UserTenant> PeekUserInfos();

        bool TryGetUserCacheItem(long userId, out UserCacheItemDto value);
        Task KeepAliveInCacheAsync(long userId);
        Task<bool> RemoveCacheItemAsync(long userId);

        long[] PeekUserIds { get; }
        bool TryPeekCacheItem(long userId, out UserCacheItemDto userCacheItemDto);

    }

    public class PQSUserCacheConfig : PQSConfig<PQSUserCacheConfig>
    {
        public double UnusedExpireTimeInMinute { get; set; }
    }

    /// <summary>
    /// Every user stores in cache an session and his tenantId.
    /// </summary>
    /// <remarks>
    /// All the active users stored in <see cref="_usersCacheDictionary"/>
    /// When a developer need to iterate all users without effecting Cache policy,  he would have to eterate over this.
    /// </remarks>
    ///  Extends <see cref="CacheItemRepositoryBase{TKey,TValue}"/>.
    public class UserSessionCacheRepository : IUserSessionCacheRepository, ISingletonDependency
    {
        private readonly ILogger<UserSessionCacheRepository> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly UserSessionFastCacheRepository _userMemCacheWrapper;

        public UserSessionCacheRepository(IOptions<PQSUserCacheConfig> config, IMemoryCache cache,
            ILogger<UserSessionCacheRepository> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            _userMemCacheWrapper = new UserSessionFastCacheRepository(cache, logger, config.Value);
        }

        public long[] PeekUserIds => _userMemCacheWrapper.PeekUserIds;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<UserTenant> PeekUserInfos()
        {
            return _userMemCacheWrapper.PeekUserInfos();
        }

        public async Task<string> GetCacheSessionAsync(long userId)
        {
            var pQSSession = string.Empty;

            if (_userMemCacheWrapper.TryGetUserCacheItem(userId, out var userDto))
            {
                if (userDto.PQSSession.IsNullOrEmpty() == false)
                {
                    pQSSession = userDto.PQSSession;
                }
            }
            else
            {
                var user = await GetUserFromDb(userId);
                if (!user.PQSSession.IsNullOrEmpty())
                {
                    await SetCacheSessionAsync(userId, user.TenantId.Value, user.PQSSession);
                }
            }

            return pQSSession;
        }

       
        public bool TryPeekCacheItem(long userId, out UserCacheItemDto userCacheItemDto)
        {
            var result = _userMemCacheWrapper.TryPeekCacheItem(userId, out userCacheItemDto);
            return result;
        }

        public async Task<bool> SetCacheSessionAsync(long userId, int tenantId, string pQSSession)
        {
            var userCacheData = new UserCacheItemDto(tenantId, pQSSession);
            await _userMemCacheWrapper.SetMemCacheItemAsync(userId, userCacheData);
            await SaveSessionIntoDbAsync(userId, userCacheData.PQSSession);

            return true;
        }


        public async Task<bool> RemoveCacheItemAsync(long userId)
        {
            UserCacheItemDto userCacheDto = default;
            if (!_userMemCacheWrapper.TryPeekCacheItem(userId, out userCacheDto))
            {
                ///We shouldnot user  <see cref="GetCacheSessionAsync"/> since then it will save in memcache and db and we dont want that!
                if (!_userMemCacheWrapper.TryGetUserCacheItem(userId, out userCacheDto))
                {
                    var user = await GetUserFromDb(userId);

                    //User Always in DB
                    userCacheDto = new UserCacheItemDto(user.TenantId ?? -1, user.PQSSession);
                }
            }
            await _userMemCacheWrapper.RemoveCacheItemAsync(userId);

            await RemoveSessionFromDb(userId);
            _logger.LogSession(userCacheDto.TenantId, userId, "Cache Removed!");

            return true;
        }

        public bool TryGetUserCacheItem(long userId, out UserCacheItemDto value)
        {
            return _userMemCacheWrapper.TryGetUserCacheItem(userId, out value);
        }

        public async Task KeepAliveInCacheAsync(long userId)
        {
            await _userMemCacheWrapper.KeepAliveInCacheAsync(userId);
        }

        private async Task<User> GetUserFromDb(long userId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
                var user = await userManager.GetUserByIdAsync(userId);

                return user;
            }
        }

        private async Task RemoveSessionFromDb(long userId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                using (var uow = unitOfWork.Begin())
                {
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
                    var user = userManager.GetUserById(userId);
                    user.PQSSession = string.Empty;

                    await userManager.UpdateAsync(user);
                    await uow.CompleteAsync();
                }
            }
        }

        private async Task SaveSessionIntoDbAsync(long userId, string session)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                using (var uow = unitOfWork.Begin())
                {
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
                    var user = userManager.GetUserById(userId);

                    user.PQSSession = session;
                    await userManager.UpdateAsync(user);
                    await uow.CompleteAsync();
                }
            }
        }
    }
}
