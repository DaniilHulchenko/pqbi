using Abp.Modules;
using Abp.Reflection.Extensions;

namespace PQBI
{
    public class PQBIClientModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(PQBIClientModule).GetAssembly());
        }
    }
}
