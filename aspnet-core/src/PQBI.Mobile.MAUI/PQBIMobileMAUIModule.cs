using Abp.AutoMapper;
using Abp.Configuration.Startup;
using Abp.Modules;
using Abp.Reflection.Extensions;
using PQBI.ApiClient;
using PQBI.Mobile.MAUI.Core.ApiClient;

namespace PQBI
{
    [DependsOn(typeof(PQBIClientModule), typeof(AbpAutoMapperModule))]

    public class PQBIMobileMAUIModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Localization.IsEnabled = false;
            Configuration.BackgroundJobs.IsJobExecutionEnabled = false;

            Configuration.ReplaceService<IApplicationContext, MAUIApplicationContext>();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(PQBIMobileMAUIModule).GetAssembly());
        }
    }
}