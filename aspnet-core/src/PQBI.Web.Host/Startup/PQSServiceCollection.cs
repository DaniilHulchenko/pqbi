using PQBI.BackgroundTasks;
using PQBI.Configuration;
using PQBI.Infrastructure;

namespace PQBI.Web.Startup
{
    public static class PQSServiceCollection
    {
        public static TClass PQSConfigure<TClass>(this IServiceCollection services, IConfiguration config) where TClass : PQSConfig<TClass>
        {
            services.Configure<TClass>(config);
            var instanceConfig = config.Get<TClass>();
            return instanceConfig;
        }
    }
}
