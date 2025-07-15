using Abp.Modules;
using Abp.Reflection.Extensions;

namespace PQBI
{
    public class PQBICoreSharedModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(PQBICoreSharedModule).GetAssembly());
        }
    }
}