using Abp.Modules;
using Abp.Reflection.Extensions;
using Castle.Windsor.MsDependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using PQBI.Configure;
using PQBI.Startup;
using PQBI.Test.Base;

namespace PQBI.GraphQL.Tests
{
    [DependsOn(
        typeof(PQBIGraphQLModule),
        typeof(PQBITestBaseModule))]
    public class PQBIGraphQLTestModule : AbpModule
    {
        public override void PreInitialize()
        {
            IServiceCollection services = new ServiceCollection();
            
            services.AddAndConfigureGraphQL();

            WindsorRegistrationHelper.CreateServiceProvider(IocManager.IocContainer, services);
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(PQBIGraphQLTestModule).GetAssembly());
        }
    }
}