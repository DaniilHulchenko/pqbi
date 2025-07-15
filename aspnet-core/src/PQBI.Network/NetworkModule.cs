using Abp.Modules;
using Abp.Reflection.Extensions;

namespace PQBI.Network
{
    public class NetworkModule : AbpModule
    {

        public NetworkModule()
        {

        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(NetworkModule).GetAssembly());
        }
    }
}
