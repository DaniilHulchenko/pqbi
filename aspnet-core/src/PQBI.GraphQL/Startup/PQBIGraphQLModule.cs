using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;

namespace PQBI.Startup
{
    [DependsOn(typeof(PQBICoreModule))]
    public class PQBIGraphQLModule : AbpModule
    {
        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(PQBIGraphQLModule).GetAssembly());
        }

        public override void PreInitialize()
        {
            base.PreInitialize();

            //Adding custom AutoMapper configuration
            Configuration.Modules.AbpAutoMapper().Configurators.Add(CustomDtoMapper.CreateMappings);
        }
    }
}