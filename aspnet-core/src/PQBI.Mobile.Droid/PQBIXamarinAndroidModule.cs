using Abp.Modules;
using Abp.Reflection.Extensions;

namespace PQBI
{
    [DependsOn(typeof(PQBIXamarinSharedModule))]
    public class PQBIXamarinAndroidModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(PQBIXamarinAndroidModule).GetAssembly());
        }
    }
}