using Abp.Dependency;

namespace PQBI.DashboardCustomization.Definitions.Cache
{
    public interface IDashboardDefinitionCacheManager : ITransientDependency
    {
        DashboardDefinition Get(string name);

        void Set(DashboardDefinition definition);
    }
}