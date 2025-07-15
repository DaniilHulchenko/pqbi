using Abp;
using Abp.Dependency;
using Abp.EntityFrameworkCore.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Zero.EntityFrameworkCore;
//using Castle.Core.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PQBI.Configuration;
using PQBI.EntityHistory;
using PQBI.Migrations.Seed;

namespace PQBI.EntityFrameworkCore
{
    [DependsOn(
        typeof(AbpZeroCoreEntityFrameworkCoreModule),
        typeof(PQBICoreModule)
    )]
    public class PQBIEntityFrameworkCoreModule : AbpModule
    {
        private readonly IConfigurationRoot _appConfiguration;
        private readonly ILogger<PQBIEntityFrameworkCoreModule> _logger;

        public PQBIEntityFrameworkCoreModule(IWebHostEnvironment env, ILogger<PQBIEntityFrameworkCoreModule> logger)
        {
            _appConfiguration = env.GetAppConfiguration();
            _logger = logger;
        }

        /* Used it tests to skip DbContext registration, in order to use in-memory database of EF Core */
        public bool SkipDbContextRegistration { get; set; }

        public bool SkipDbSeed { get; set; }

        public override void PreInitialize()
        {
            var pqbiSection = _appConfiguration.GetSection(PqbiConfig.ApiName);
            var pqbiConfig = pqbiSection.Get<PqbiConfig>();


            //_logger.LogInformation($"PreInitialize xxxxxxxxx pqbiConfig.MultiTenancyEnabled = {pqbiConfig.MultiTenancyEnabled} pqbiConfig.MultiTenancyEnabled == false = [{pqbiConfig.MultiTenancyEnabled == false}]");
            if (!SkipDbContextRegistration)
            {
                Configuration.Modules.AbpEfCore().AddDbContext<PQBIDbContext>(options =>
                {
                    if (options.ExistingConnection != null)
                    {
                        PQBIDbContextConfigurer.Configure(options.DbContextOptions, options.ExistingConnection, pqbiConfig.MultiTenancyEnabled);
                    }
                    else
                    {
                        PQBIDbContextConfigurer.Configure(options.DbContextOptions, options.ConnectionString, pqbiConfig.MultiTenancyEnabled);
                    }
                });
            }
            //SQLite doesn't support multithreading, transactions should be disabled
            Configuration.UnitOfWork.IsTransactional = false;
            // Set this setting to true for enabling entity history.
            Configuration.EntityHistory.IsEnabled = false;

            // Uncomment below line to write change logs for the entities below:
            // Configuration.EntityHistory.Selectors.Add("PQBIEntities", EntityHistoryHelper.TrackedTypes);
            // Configuration.CustomConfigProviders.Add(new EntityHistoryConfigProvider(Configuration));
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(PQBIEntityFrameworkCoreModule).GetAssembly());
        }

        public override void PostInitialize()
        {
            var configurationAccessor = IocManager.Resolve<IAppConfigurationAccessor>();
            var appConfiguration = configurationAccessor.Configuration;
            var pqbiSection = appConfiguration.GetSection(PqbiConfig.ApiName);
            var pqbiConfig = pqbiSection.Get<PqbiConfig>();

            using (var scope = IocManager.CreateScope())
            {
                if (!SkipDbSeed && scope.Resolve<DatabaseCheckHelper>().Exist(configurationAccessor.Configuration["ConnectionStrings:Default"]))
                {
                    SeedHelper.SeedHostDb(IocManager, pqbiConfig);

                }
            }
        }
    }
}