using PQBI.Infrastructure;

namespace PQSServiceMock.Startup
{
    public static class PQSServiceMockCollection
    {
        public static TClass PQSConfigure2<TClass>(this IServiceCollection services, IConfiguration config) where TClass : PQSConfig<TClass>
        {
            services.Configure<TClass>(config);
            var instanceConfig = config.Get<TClass>();
            return instanceConfig;
        }
    }
}
