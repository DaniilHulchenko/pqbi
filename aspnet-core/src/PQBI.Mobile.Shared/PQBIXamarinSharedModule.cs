using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;

namespace PQBI
{
    [DependsOn(typeof(PQBIClientModule), typeof(AbpAutoMapperModule))]
    public class PQBIXamarinSharedModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Localization.IsEnabled = false;
            Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(PQBIXamarinSharedModule).GetAssembly());
        }
    }
}