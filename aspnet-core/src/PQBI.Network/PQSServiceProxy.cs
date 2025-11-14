using Abp.Dependency;
using Abp.Extensions;
using Abp.UI;
using Castle.Core.Internal;
using Castle.Core.Logging;
using GrpcService1;
using Microsoft.Extensions.Logging;
using PQBI.Caching;
using PQBI.Configuration;
using PQBI.Infrastructure;
using PQBI.Network.Base;
using PQBI.Network.Grpc;
using PQBI.Network.RestApi;
using PQBI.Requests;

namespace PQBI.Network
{
    public interface IPQSServiceProxy
    {
        Task<PQSGetSessionResponse> OpenAuthenticateAsync(int tenantId, string url, string userName, string password);

        Task<bool> CloseSessionForUserAsync(long userId, int tenantId);

        Task<bool> SendNOPForUserAsync(long userId, string url);

        Task<string> RequestXmlAsync(long userId, string url, RequestString request);
        Task KeepAliveAsync(long userId);
        Task<string> GetUserRole(string session, string url, string userName);
    }


    public class PQSServiceProxy : IPQSServiceProxy, ITransientDependency
    {
        private readonly ITenantCacheRepository _tenantCacheRepository;
        private readonly IUserSessionCacheRepository _userSessionCacheRepository;
        private readonly IPQSRestApiService _pQSestApiService;
        private readonly ILogger<PQSServiceProxy> _logger;

        public PQSServiceProxy(ITenantCacheRepository tenantCacheRepository, IUserSessionCacheRepository userSessionCacheRepository,
            IPQSRestApiService pQSestApiService,
            ILogger<PQSServiceProxy> logger)
        {
            _tenantCacheRepository = tenantCacheRepository;
            _userSessionCacheRepository = userSessionCacheRepository;
            _pQSestApiService = pQSestApiService;
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns>Session Id</returns>
        public async Task<PQSGetSessionResponse> OpenAuthenticateAsync(int tenantId, string url, string userName, string password)
        {
            _logger.LogInformation($"PQSServiceProxy | OpenAuthenticateAsync url = {url}");
            var service = await GetCommunicationServiceByTenantAsync(tenantId);
            var sessionResponse = await service.OpenSessionForUserAsync(url, userName, password);

            return sessionResponse;
        }


        public async Task<string> GetUserRole(string session, string url, string userName)
        {            
            return await _pQSestApiService.GetUserRole(session, url, userName);           
        }


        public async Task<bool> CloseSessionForUserAsync(long userId, int tenantId)
        {
            var result = false;
            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantId);

            var session = await _userSessionCacheRepository.GetCacheSessionAsync(userId);
            if (!session.IsNullOrEmpty())
            {
                var service = await GetCommunicationServiceByTenantAsync(tenantId);
                result = await service.CloseSessionForUserAsync(tenant.PQSServiceUrl, session);

                await _userSessionCacheRepository.RemoveCacheItemAsync(userId);
            }

            return result;
        }

        public async Task<bool> SendNOPForUserAsync(long userId, string url)
        {
            var result = false;
            if (_userSessionCacheRepository.TryPeekCacheItem(userId, out var userCacheItem))
            {
                try
                {
                    var service = await GetCommunicationServiceByTenantAsync(userCacheItem.TenantId);
                    result = await service.SendNOPForUserAsync(url, userCacheItem.PQSSession);
                }
                catch (Exception)
                {
                    //TODO: Need to implement exception mechanism first.
                }
            }

            return result;
        }

        public async Task<string> RequestXmlAsync(long userId, string url, RequestString request)
        {
            try
            {
                if (_userSessionCacheRepository.TryGetUserCacheItem(userId, out var userCacheItem) == false)
                {
                    throw new UserFriendlyException("Please sign in later");
                }

                var service = await GetCommunicationServiceByTenantAsync(userCacheItem.TenantId);
                var response = await service.RequestXmlAsync(url, request.Message);

                if (string.IsNullOrEmpty(response))
                {
                    //TODO: Should be implemented in PQBI-14.
                    throw new UserFriendlyException($"{nameof(RequestXmlAsync)} failed");
                }

                return response;
            }
            catch (UserFriendlyException e)
            {
                //TODO: Should be implemented in PQBI-14.
                throw new UserFriendlyException(e.Message);
            }
        }

        public async Task KeepAliveAsync(long userId)
        {
            await _userSessionCacheRepository.KeepAliveInCacheAsync(userId);
        }

        private async Task<IPQSRestApiServiceBase> GetCommunicationServiceByTenantAsync(int tenantID)
        {
            var tenant = await _tenantCacheRepository.GetTenantByIdAsync(tenantID);
            return GetCommunicationType(tenant.PQSCommunitcationType);
        }

        private IPQSRestApiServiceBase GetCommunicationType(PQSCommunitcationType communitcationType)
        {
            switch (communitcationType)
            {
                case PQSCommunitcationType.RestApi:
                    return _pQSestApiService;

                default:
                    throw new NotImplementedException("Only Grpc or RestApi supported.");
            }
        }
    }
}
