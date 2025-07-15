using Abp.Modules;
using Abp.Reflection.Extensions;

namespace PQBI
{
    [DependsOn(typeof(PQBIXamarinSharedModule))]
    public class PQBIXamarinIosModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(PQBIXamarinIosModule).GetAssembly());
        }
    }
}