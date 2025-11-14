using System.Reflection;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.Mvc.Antiforgery;
using Abp.AspNetCore.Mvc.Extensions;
using Abp.AspNetCore.SignalR.Hubs;
using Abp.AspNetZeroCore.Web.Authentication.JwtBearer;
using Abp.Castle.Logging.Log4Net;
using Abp.Extensions;
using Abp.Hangfire;
using Abp.PlugIns;
using Castle.Facilities.Logging;
using Hangfire;
using PQBI.Authorization;
using PQBI.Configuration;
using PQBI.EntityFrameworkCore;
using PQBI.Identity;
using PQBI.Web.Chat.SignalR;
using PQBI.Web.Common;
using PQBI.Web.Swagger;
using Stripe;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using GraphQL.Server.Ui.Playground;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using PQBI.Configure;
using PQBI.Schemas;
using PQBI.Web.HealthCheck;
using Owl.reCAPTCHA;
using HealthChecksUISettings = HealthChecks.UI.Configuration.Settings;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using PQBI.Web.MultiTenancy;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using PQBI.BackgroundTasks;
using PQBI.Network.RestApi;
using PQBI.Network.Grpc;
using GrpcService1;
using Serilog;
using PQBI.Caching;
using PQBI.Trace;
using PQBI.Web.Infrastructures;
using PQBI.Network.Base.Policies;
using PQBI.Web.Middlewares;
using PQBI.Web.Models;
using PQBI.PQS;
using Abp.HtmlSanitizer;
using Microsoft.AspNetCore.Authentication.Cookies;
using PQBI.Web.OpenIddict;
using Serilog.Formatting.Json;
using PQBI.CalculationEngine;
using PQBI.Network.RestApi.EngineCalculation;
using PQBI.Network.RestApi.Validations;
using Abp.Dependency;
using Abp.Events.Bus.Exceptions;
using Abp.Events.Bus.Handlers;
using System.Diagnostics;
using PQBI.Infrastructure;

namespace PQBI.Web.Startup
{
    public class MyExceptionHandler : IEventHandler<AbpHandledExceptionData>, ITransientDependency
    {
        public void HandleEvent(AbpHandledExceptionData eventData)
        {
            //TODO: Check eventData.Exception!
        }
    }

    public class Startup
    {
        private const string DefaultCorsPolicyName = "localhost";

        private readonly IConfigurationRoot _appConfiguration;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public Startup(IWebHostEnvironment env)
        {
            _hostingEnvironment = env;
            _appConfiguration = env.GetAppConfiguration();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            //Debugger.Launch();


            Console.WriteLine($"Xxxxx  env.EnvironmentName = {_hostingEnvironment.EnvironmentName}   env.IsDevelopment() = {_hostingEnvironment.IsDevelopment()}");

            var variableWriter = new EnvironmentVariableWriter();
            services.AddSingleton<ClientPolicy>();

            //MVC
            var mvcBuilder = services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AbpAutoValidateAntiforgeryTokenAttribute());
                options.AddAbpHtmlSanitizer();
            });
#if DEBUG
            mvcBuilder.AddRazorRuntimeCompilation();
#endif

            services.AddSignalR();

            services.AddSingleton<IPQZBinaryWriterWrapper>(x => new PQZBinaryWriterWrapper());

            //Configure CORS for angular2 UI
            services.AddCors(options =>
            {
                options.AddPolicy(DefaultCorsPolicyName, builder =>
                {
                    //App:CorsOrigins in appsettings.json can contain more than one address with splitted by comma.
                    builder
                        .WithOrigins(
                            // App:CorsOrigins in appsettings.json can contain more than one address separated by comma.
                            _appConfiguration["App:CorsOrigins"]
                                .Split(",", StringSplitOptions.RemoveEmptyEntries)
                                .Select(o => o.RemovePostFix("/"))
                                .ToArray()
                        )
                        .SetIsOriginAllowedToAllowWildcardSubdomains()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            if (bool.Parse(_appConfiguration["KestrelServer:IsEnabled"]))
            {
                ConfigureKestrel(services);
            }

            IdentityRegistrar.Register(services);
            AuthConfigurer.Configure(services, _appConfiguration);

            if (bool.Parse(_appConfiguration["OpenIddict:IsEnabled"]))
            {
                OpenIddictRegistrar.Register(services, _appConfiguration);

                services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme,
                    options => { options.LoginPath = "/Ui/Login"; });
            }
            else
            {
                services.Configure<SecurityStampValidatorOptions>(opts =>
                {
                    opts.OnRefreshingPrincipal = SecurityStampValidatorCallback.UpdatePrincipal;
                });
            }

            var trendSection = _appConfiguration.GetSection(TrendConfig.ApiName);
            var nopSection = _appConfiguration.GetSection(NopSessionConfig.ApiName);
            var pQsCommunicationSection = _appConfiguration.GetSection(PQSComunication.ApiName);
            var pQdUserCacheConfig = _appConfiguration.GetSection(PQSUserCacheConfig.ApiName);
            var logWatcherSection = _appConfiguration.GetSection(LogWatcherConfig.ApiName);
            var clientPolicySection = _appConfiguration.GetSection(ClientPolicyConfig.ApiName);
            var seqSection = _appConfiguration.GetSection(SeqConfig.ApiName);
            var pqbiSection = _appConfiguration.GetSection(PqbiConfig.ApiName);
            var taskOrchestratorSection = _appConfiguration.GetSection(TaskOrchestratorConfig.ApiName);
            var engineCalculationSection = _appConfiguration.GetSection(FunctionEngineConfig.ApiName);


            var configurationService = new PQSConfigurationService();
            services.AddSingleton<IPQSConfigurationService, PQSConfigurationService>(serviceProvider =>
            {
                return configurationService;
            });



            configurationService.AddConfig(services.PQSConfigure<TrendConfig>(trendSection));
            configurationService.AddConfig(services.PQSConfigure<NopSessionConfig>(nopSection));
            configurationService.AddConfig(services.PQSConfigure<PQSComunication>(pQsCommunicationSection));
            configurationService.AddConfig(services.PQSConfigure<PQSUserCacheConfig>(pQdUserCacheConfig));
            configurationService.AddConfig(services.PQSConfigure<LogWatcherConfig>(logWatcherSection));
            configurationService.AddConfig(services.PQSConfigure<ClientPolicyConfig>(clientPolicySection));
            configurationService.AddConfig(services.PQSConfigure<SeqConfig>(seqSection));
            configurationService.AddConfig(services.PQSConfigure<PqbiConfig>(pqbiSection));
            configurationService.AddConfig(services.PQSConfigure<TaskOrchestratorConfig>(taskOrchestratorSection));
            configurationService.AddConfig(services.PQSConfigure<FunctionEngineConfig>(engineCalculationSection));



            var pQsCommunication = pQsCommunicationSection.Get<PQSComunication>();
            var seqConfig = seqSection.Get<SeqConfig>();
            var pqbiConfig = pqbiSection.Get<PqbiConfig>();

            PQBIConsts.MultiTenancyEnabled = pqbiConfig.MultiTenancyEnabled;

            services.AddHostedService<NopBackgroundTask>();

            //Identity server
            //if (bool.Parse(_appConfiguration["IdentityServer:IsEnabled"]))
            //{
            //    OpenIddictRegistrar.Register(services, _appConfiguration);

            //    services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme,
            //        options => { options.LoginPath = "/Ui/Login"; });
            //}
            //else
            //{
            services.Configure<SecurityStampValidatorOptions>(opts =>
            {
                opts.OnRefreshingPrincipal = SecurityStampValidatorCallback.UpdatePrincipal;
            });
            //}

            if (WebConsts.SwaggerUiEnabled)
            {
                //Swagger - Enable this line and the related lines in Configure method to enable swagger UI
                ConfigureSwagger(services);
            }

            //Recaptcha
            services.AddreCAPTCHAV3(x =>
            {
                x.SiteKey = _appConfiguration["Recaptcha:SiteKey"];
                x.SiteSecret = _appConfiguration["Recaptcha:SecretKey"];
            });

            if (WebConsts.HangfireDashboardEnabled)
            {
                //Hangfire(Enable to use Hangfire instead of default job manager)
                services.AddHangfire(config =>
                {
                    config.UseSqlServerStorage(_appConfiguration.GetConnectionString("Default"));
                });

                services.AddHangfireServer();
            }

            if (WebConsts.GraphQL.Enabled)
            {
                services.AddAndConfigureGraphQL();
            }

            if (bool.Parse(_appConfiguration["HealthChecks:HealthChecksEnabled"]))
            {
                ConfigureHealthChecks(services);
            }

            services.AddMemoryCache();
            var watcherService = new LogWatcherService(logWatcherSection.Get<LogWatcherConfig>());
            services.AddSingleton<ILogWatcherService>(serviceProvider =>
            {
                return watcherService;
            });

            services.AddTransient<IPQSTreeBuilderService, PQSTreeBuilderService>();
            services.AddTransient<IFeederChannelTredBuilder,FeederChannelTredBuilder>();
            
            //services.AddTransient<IPQSGrpcService, PQSGrpcService>();
            services.AddTransient<IPQSRestApiService, PQSRestApiBinaryService>();
            services.AddTransient<IPQSComponentOperationService, PQSComponentOperationService>();
            services.AddTransient<ICustomParameterCalculationService, CustomParameterCalculationService>();
            //services.AddTransient<IPQSTrendDataValidationService, PQSTrendDataValidationService>();
            services.AddTransient<IEngineCalculationService, EngineCalculationService>();
            services.AddTransient<IPQSenderHelper, PQSenderHelper>();
            services.AddTransient<IFunctionEngine, FunctionEngine>();


            services.AddSingleton<ITaskOrchestrator, TaskOrchestrator>();

            services.AddHttpClient(IPQSRestApiService.Alias)
                .AddPolicyHandler((serviceProvider, response) =>
                {
                    //var clientPolicy = serviceProvider.GetRequiredService<ClientPolicy>();
                    //return clientPolicy.ImediateHttpRetry;

                    var clientPolicy = serviceProvider.GetRequiredService<ClientPolicy>();
                    return clientPolicy.PolicyWrap;
                })
                .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler);


            //services.AddGrpcClient<PQSCommunication.PQSCommunicationClient>(o =>
            //{
            //    o.Address = new Uri(pQsCommunication.PQSServiceGrpcUrl);

            //}).AddPolicyHandler((serviceProvider, response) =>
            //    {
            //        var clientPolicy = serviceProvider.GetRequiredService<ClientPolicy>();
            //        return clientPolicy.PolicyWrap;
            //    })
            //    .ConfigurePrimaryHttpMessageHandler(ConfigurePrimaryHttpMessageHandler);


            HttpMessageHandler ConfigurePrimaryHttpMessageHandler()
            {
                Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> sslCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                if (pQsCommunication.IsAllCertificatesTrusted == false)
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

            //Configure Abp and Dependency Injection
            return services.AddAbp<PQBIWebHostModule>(options =>
            {

                //Configure Log4Net logging
                //options.IocManager.IocContainer.AddFacility<LoggingFacility>(
                //    f => f.UseAbpLog4Net().WithConfig(_hostingEnvironment.IsDevelopment()
                //        ? "log4net.config"
                //        : "log4net.Production.config")
                //);


                var logDirectory = variableWriter.LOG_FILE_PATH ?? @"Logs\";
                var logFileTxt = $"{logDirectory}log.txt";
                var logFileJson = $"{logDirectory}log.json";
                var seqHost = variableWriter.SEQ_HOST_URL ?? seqConfig.Url;
                var referer = variableWriter.BUILDER_REFERER ?? Environment.MachineName;

                var config = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithProperty("Referer", referer)
                    .WriteTo.Console()
                    .WriteTo.File(logFileTxt, rollingInterval: RollingInterval.Day, outputTemplate: "")
                    .WriteTo.File(new JsonFormatter(), logFileJson, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
                    .WriteTo.Seq(seqHost) //Seq is an external process
                    .WriteTo.CustomSink(watcherService)
                    .CreateLogger();


                options.IocManager.IocContainer.AddFacility<LoggingFacility>(f => f.LogUsing(new AdapterSerilogFactory(config)));



                options.PlugInSources.AddFolder(Path.Combine(_hostingEnvironment.WebRootPath, "Plugins"),
                    SearchOption.AllDirectories);


                variableWriter.WriteAllVaribles();
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        
        {
            //Initializes ABP framework.
            app.UseAbp(options =>
            {
                options.UseAbpRequestLocalization = false; //used below: UseAbpRequestLocalization
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseHsts();
            }
            else
            {
                app.UseStatusCodePagesWithRedirects("~/Error?statusCode={0}");
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            if (PQBIConsts.PreventNotExistingTenantSubdomains)
            {
                app.UseMiddleware<DomainTenantCheckMiddleware>();
            }

            app.UseRouting();

            app.UseCors(DefaultCorsPolicyName); //Enable CORS!

            app.UseAuthentication();
            app.UseJwtTokenMiddleware();

            if (bool.Parse(_appConfiguration["OpenIddict:IsEnabled"]))
            {
                app.UseAbpOpenIddictValidation();
            }

            app.UseAuthorization();

            using (var scope = app.ApplicationServices.CreateScope())
            {
                if (scope.ServiceProvider.GetService<DatabaseCheckHelper>()
                    .Exist(_appConfiguration["ConnectionStrings:Default"]))
                {
                    app.UseAbpRequestLocalization();
                }
            }

            if (WebConsts.HangfireDashboardEnabled)
            {
                //Hangfire dashboard &server(Enable to use Hangfire instead of default job manager)
                app.UseHangfireDashboard(WebConsts.HangfireDashboardEndPoint, new DashboardOptions
                {
                    Authorization = new[]
                        {new AbpHangfireAuthorizationFilter(AppPermissions.Pages_Administration_HangfireDashboard)}
                });
            }

            if (bool.Parse(_appConfiguration["Payment:Stripe:IsActive"]))
            {
                StripeConfiguration.ApiKey = _appConfiguration["Payment:Stripe:SecretKey"];
            }

            if (WebConsts.GraphQL.Enabled)
            {
                app.UseGraphQL<MainSchema>(WebConsts.GraphQL.EndPoint);
                if (WebConsts.GraphQL.PlaygroundEnabled)
                {
                    // to explorer API navigate https://*DOMAIN*/ui/playground
                    app.UseGraphQLPlayground(
                        WebConsts.GraphQL.PlaygroundEndPoint,
                        new PlaygroundOptions()
                    );
                }
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<AbpCommonHub>("/signalr");
                endpoints.MapHub<ChatHub>("/signalr-chat");

                app.UseMiddleware<UserKeepAliveInCacheMiddleware>();


                endpoints.MapControllerRoute("defaultWithArea", "{area}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

                app.ApplicationServices.GetRequiredService<IAbpAspNetCoreConfiguration>().EndpointConfiguration
                    .ConfigureAllEndpoints(endpoints);
            });

            if (bool.Parse(_appConfiguration["HealthChecks:HealthChecksEnabled"]))
            {
                app.UseHealthChecks("/health", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                if (bool.Parse(_appConfiguration["HealthChecks:HealthChecksUI:HealthChecksUIEnabled"]))
                {
                    app.UseHealthChecksUI();
                }
            }

            if (WebConsts.SwaggerUiEnabled)
            {
                // Enable middleware to serve generated Swagger as a JSON endpoint
                app.UseSwagger();
                // Enable middleware to serve swagger-ui assets (HTML, JS, CSS etc.)

                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint(_appConfiguration["App:SwaggerEndPoint"], "PQBI API V1");
                    options.IndexStream = () => Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("PQBI.Web.wwwroot.swagger.ui.index.html");
                    options.InjectBaseUrl(_appConfiguration["App:ServerRootAddress"]);
                }); //URL: /swagger
            }
        }

        private void ConfigureKestrel(IServiceCollection services)
        {
            services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
            {
                options.Listen(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 443),
                    listenOptions =>
                    {
                        var certPassword = _appConfiguration.GetValue<string>("Kestrel:Certificates:Default:Password");
                        var certPath = _appConfiguration.GetValue<string>("Kestrel:Certificates:Default:Path");
                        var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath,
                            certPassword);
                        listenOptions.UseHttps(new HttpsConnectionAdapterOptions()
                        {
                            ServerCertificate = cert
                        });
                    });
            });
        }

        private void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo() { Title = "PQBI API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.ParameterFilter<SwaggerEnumParameterFilter>();
                options.SchemaFilter<SwaggerEnumSchemaFilter>();
                options.OperationFilter<SwaggerOperationIdFilter>();
                options.OperationFilter<SwaggerOperationFilter>();
                options.CustomDefaultSchemaIdSelector();

                //add summaries to swagger
                bool canShowSummaries = _appConfiguration.GetValue<bool>("Swagger:ShowSummaries");
                if (canShowSummaries)
                {
                    var hostXmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var hostXmlPath = Path.Combine(AppContext.BaseDirectory, hostXmlFile);
                    options.IncludeXmlComments(hostXmlPath);

                    var applicationXml = $"PQBI.Application.xml";
                    var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, applicationXml);
                    options.IncludeXmlComments(applicationXmlPath);

                    var webCoreXmlFile = $"PQBI.Web.Core.xml";
                    var webCoreXmlPath = Path.Combine(AppContext.BaseDirectory, webCoreXmlFile);
                    options.IncludeXmlComments(webCoreXmlPath);
                }
            });
        }

        private void ConfigureHealthChecks(IServiceCollection services)
        {
            services.AddAbpZeroHealthCheck();

            var healthCheckUISection = _appConfiguration.GetSection("HealthChecks")?.GetSection("HealthChecksUI");

            if (bool.Parse(healthCheckUISection["HealthChecksUIEnabled"]))
            {
                services.Configure<HealthChecksUISettings>(settings =>
                {
                    healthCheckUISection.Bind(settings, c => c.BindNonPublicProperties = true);
                });
                services.AddHealthChecksUI()
                    .AddInMemoryStorage();
            }
        }
    }
}
