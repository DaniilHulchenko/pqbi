using Microsoft.FeatureManagement;
using PQBI.Configuration;
using PQBI.Infrastructure;
using PQBI.Network.RestApi;
using PQBI.PQS;
using PQSServiceMock.Services;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace PQSServiceMock.Startup;

public static class DependencyRegistrar
{

    public static IServiceCollection RegisterAllServices(this IServiceCollection services, IWebHostEnvironment env)
    {
        var appConfig = env.GetAppConfiguration();
        services.RegisterCustomServices(appConfig);
        services.RegisterStandartServices();

        return services;
    }

    public static IServiceCollection RegisterCustomServices(this IServiceCollection services, IConfigurationRoot configurationRoot)
    {
        services.AddTransient<IPQSRestApiService, PQSRestApiBinaryService>();


        PQSComunication pQsCommunicationConfig = null;

        services.AddSingleton<IPQZBinaryWriterWrapper>(x => new PQZBinaryWriterWrapper());
        services.AddSingleton<IPQSServiceResponse, PQSUpResponses>();
        services.AddSingleton<IPQSServiceAutoResponseManager, PQSServiceAutoResponseManager>();


        services.AddTransient<IPQSenderHelper, PQSenderHelper>();




        services.AddHttpClient(IPQSRestApiService.Alias)

               .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler);


        var pQsCommunicationSection = configurationRoot.GetSection(PQSComunication.ApiName);

        var configurationService = new PQSConfigurationService();
        services.AddSingleton<IPQSConfigurationService, PQSConfigurationService>(serviceProvider =>
        {
            return configurationService;
        });

        pQsCommunicationConfig = services.PQSConfigure2<PQSComunication>(pQsCommunicationSection);


        services.PQSConfigure<PQSComunication>(pQsCommunicationSection);


        HttpMessageHandler ConfigurePrimaryHttpMessageHandler()
        {
            Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> sslCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            if (pQsCommunicationConfig.IsAllCertificatesTrusted == false)
            {
                sslCallback = (HttpRequestMessage requestMessage, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslErrors) =>
                {
                    var result = sslErrors == SslPolicyErrors.None;
                    return result;
                };
            }

            var httpHandler = new HttpClientHandler() { ServerCertificateCustomValidationCallback = sslCallback };
            return httpHandler;
        }


        return services;
    }

    public static IServiceCollection RegisterStandartServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddFeatureManagement();

        return services;
    }

    public static TClass PQSConfigure<TClass>(this IServiceCollection services, IConfiguration config) where TClass : PQSConfig<TClass>
    {
        services.Configure<TClass>(config);
        var instanceConfig = config.Get<TClass>();
        return instanceConfig;
    }
}
