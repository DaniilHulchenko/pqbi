using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PQBI.Caching;
using PQBI.Infrastructure;
using PQBI.Infrastructure.Extensions;
using PQBI.Network;

namespace PQBI.BackgroundTasks
{
    public class NopSessionConfig : PQSConfig<NopSessionConfig>
    {
        public int IntervalInSeconds { get; set; }
    }

    public class NopBackgroundTask : BackgroundService
    {
        private readonly NopSessionConfig _config;
        private readonly ILogger<NopBackgroundTask> _logger;
        private readonly IUserSessionCacheRepository _userSessionCacheRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITenantCacheRepository _tenantCacheRepository;

        public NopBackgroundTask(
            IOptions<NopSessionConfig> config,
            ILogger<NopBackgroundTask> logger,
            IUserSessionCacheRepository userSessionCacheRepository,
            IServiceProvider serviceProvider,
            ITenantCacheRepository tenantCacheRepository)
        {

            _config = config.Value;
            _logger = logger;
            _userSessionCacheRepository = userSessionCacheRepository;
            _serviceProvider = serviceProvider;
            _tenantCacheRepository = tenantCacheRepository;
        }


        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var delayInSecond = _config.IntervalInSeconds * 1000;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var userInfos = _userSessionCacheRepository.PeekUserInfos();
                    if (userInfos.IsCollectionEmpty())
                    {
                        _logger.LogInformation("Nop_NopBackgroundTask No Cache");
                    }
                    else
                    {
                        var serialized = JsonConvert.SerializeObject(userInfos);
                        _logger.LogInformation("Nop_NopBackgroundTask {@nop_users}", userInfos);
                    }

                    if (userInfos.Any())
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            foreach (var userInfo in userInfos)
                            {
                                var userSessionManager = scope.ServiceProvider.GetRequiredService<IPQSServiceProxy>();
                                var tenant = await _tenantCacheRepository.GetTenantByIdAsync(userInfo.TenantId);

                                var result = await userSessionManager.SendNOPForUserAsync(userInfo.UserId, tenant.PQSServiceUrl);

                                if (result == true)
                                {
                                    userSessionManager.KeepAliveAsync(userInfo.UserId);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {

                }

                await Task.Delay(delayInSecond, stoppingToken);
            }
        }
    }
}
