using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using PQBI.Authorization;

namespace PQBI
{
    /// <summary>
    /// Application layer module of the application.
    /// </summary>
    [DependsOn(
        typeof(PQBIApplicationSharedModule),
        typeof(PQBICoreModule)
        )]
    public class PQBIApplicationModule : AbpModule
    {
        public override void PreInitialize()
        {
            //Adding authorization providers
            Configuration.Authorization.Providers.Add<AppAuthorizationProvider>();

            //Adding custom AutoMapper configuration
            Configuration.Modules.AbpAutoMapper().Configurators.Add(CustomDtoMapper.CreateMappings);
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(PQBIApplicationModule).GetAssembly());
        }
    }
}