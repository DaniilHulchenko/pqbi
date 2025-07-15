using Abp.Modules;
using Abp.Reflection.Extensions;

namespace PQBI
{
    [DependsOn(typeof(PQBICoreSharedModule))]
    public class PQBIApplicationSharedModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(PQBIApplicationSharedModule).GetAssembly());
        }
    }
}