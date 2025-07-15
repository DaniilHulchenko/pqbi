using System;
using System.Transactions;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore.Uow;
using Abp.MultiTenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PQBI.Configuration;
using PQBI.EntityFrameworkCore;
using PQBI.Migrations.Seed.Host;
using PQBI.Migrations.Seed.Tenants;

namespace PQBI.Migrations.Seed
{
    public static class SeedHelper
    {
        public static void SeedHostDb(IIocResolver iocResolver, PqbiConfig pqbiConfig)
        {
            WithDbContext<PQBIDbContext>(iocResolver, pqbiConfig, SeedTenantDb);
            //WithDbContext<PQBIDbContext>(iocResolver, SeedHostDb);
        }

        public static void SeedHostDb(PQBIDbContext context)
        {
            context.SuppressAutoSetTenantId = true;

            //Host seed
            new InitialHostDbBuilder(context).Create();
        }


        //public static void SeedTenantDb(PQBIDbContext context, PqbiConfig pqbiConfig)
        public static void SeedTenantDb(PQBIDbContext context, PqbiConfig pqbiConfig, ILogger<PQBIEntityFrameworkCoreModule> logger)
        {
            context.SuppressAutoSetTenantId = true;

            //Host seed
            new InitialHostDbBuilder(context).Create();

            if (pqbiConfig.MultiTenancyEnabled == false)
            {

                //Default tenant seed (in host database).
                new DefaultTenantBuilder(context, pqbiConfig.TenantSeedConfig, logger).Create();
                new TenantRoleAndUserBuilder(context, 1, pqbiConfig.TenantSeedConfig).Create();
            }
        }




        private static void WithDbContext<TDbContext>(IIocResolver iocResolver, PqbiConfig pqbiConfig, Action<TDbContext, PqbiConfig, ILogger<PQBIEntityFrameworkCoreModule>> contextAction)
            where TDbContext : DbContext
        {
            using (var uowManager = iocResolver.ResolveAsDisposable<IUnitOfWorkManager>())
            {
                using (var uow = uowManager.Object.Begin(TransactionScopeOption.Suppress))
                {
                    var context = uowManager.Object.Current.GetDbContext<TDbContext>(MultiTenancySides.Host);
                    var logger = iocResolver.Resolve<ILogger<PQBIEntityFrameworkCoreModule>>();

                    var env = iocResolver.Resolve<IWebHostEnvironment>();

                    try
                    {
                        if (!env.IsDevelopment())
                        {
                            logger.LogError("Migration activated");
                            context.Database.Migrate();
                        }

                    }
                    catch (Exception ex)
                    {

                        logger.LogError(ex, "An error occurred while applying database migrations.");
                        throw;
                    }

                    contextAction(context, pqbiConfig, logger);

                    uow.Complete();
                }
            }
        }


        private static void WithDbContext<TDbContext>(IIocResolver iocResolver, Action<TDbContext> contextAction)
           where TDbContext : DbContext
        {
            using (var uowManager = iocResolver.ResolveAsDisposable<IUnitOfWorkManager>())
            {
                using (var uow = uowManager.Object.Begin(TransactionScopeOption.Suppress))
                {
                    var context = uowManager.Object.Current.GetDbContext<TDbContext>(MultiTenancySides.Host);

                    contextAction(context);

                    uow.Complete();
                }
            }
        }
    }
}
