using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.ObjectMapping;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PQBI.Configuration;
using PQBI.Infrastructure;
using PQBI.MultiTenancy;
using System;
using System.Threading.Tasks;

namespace PQBI.Caching
{
    public class TenantWorkItemDto : ITenantObjectMapperClonable
    {
        public string PQSServiceUrl { get; set; } = string.Empty;
        public PQSCommunitcationType PQSCommunitcationType { get; set; }
    }

    public interface ITenantCacheRepository
    {
        Task<TenantWorkItemDto> GetTenantByIdAsync(int tetantId);
    }

    public class TenantCacheRepository : CacheItemRepositoryBase<int, TenantWorkItemDto>, ITenantCacheRepository, ISingletonDependency
    {
        private readonly IObjectMapper _objectMapper;
        private readonly IServiceProvider _serviceProvider;

        public TenantCacheRepository(IMemoryCache cache,
            ILogger<TenantCacheRepository> logger,
            IObjectMapper objectMapper,
            IServiceProvider serviceProvider) : base(cache, logger)
        {
            _objectMapper = objectMapper;
            _serviceProvider = serviceProvider;
        }

        public override TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromHours(24);

        public override async Task SetMemCacheItemAsync(int tenantId, TenantWorkItemDto tenantData)
        {
            if (tenantData != null)
            {
                await base.SetMemCacheItemAsync(tenantId, tenantData);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                    using (var uow = unitOfWork.Begin())
                    {
                        var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();

                        var tenant = tenantManager.GetById(tenantId);
                        tenant.PQSServiceUrl = tenantData.PQSServiceUrl;
                        tenant.PQSCommunitcationType = tenantData.PQSCommunitcationType;

                        tenantManager.Update(tenant);
                        uow.Complete();
                    }
                }
            }
        }

        public async Task<TenantWorkItemDto> GetTenantByIdAsync(int tenantId)
        {
            var tenantDto = default(TenantWorkItemDto);

            if (!TryGetUserCacheItem(tenantId, out tenantDto))
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
                    var tenant = tenantManager.GetById(tenantId);

                    tenantDto = _objectMapper.Map<TenantWorkItemDto>(tenant);
                }

                await SetMemCacheItemAsync(tenantId, tenantDto);
            }

            return tenantDto;
        }

    }
}
