using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.Mvc.Antiforgery;
using Abp.AspNetCore.Mvc.Extensions;
using Abp.AspNetCore.SignalR.Hubs;
using Abp.AspNetZeroCore.Web.Authentication.JwtBearer;
using Abp.Castle.Logging.Log4Net;
using Abp.Dependency;
using Abp.Events.Bus.Exceptions;
using Abp.Events.Bus.Handlers;
using Abp.Extensions;
using Abp.Hangfire;
using Abp.HtmlSanitizer;
using Abp.PlugIns;
using Castle.Facilities.Logging;
using Google.Protobuf.Collections;
using GraphQL.Server.Ui.Playground;
using GrpcService1;
using Hangfire;
using HealthChecks.UI.Client;
using IdentityModel;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Owl.reCAPTCHA;
using PQBI.Authorization;
using PQBI.BackgroundTasks;
using PQBI.Caching;
using PQBI.CalculationEngine;
using PQBI.Configuration;
using PQBI.Configure;
using PQBI.EntityFrameworkCore;
using PQBI.Identity;
using PQBI.Infrastructure;
using PQBI.Network.Base.Policies;
using PQBI.Network.Grpc;
using PQBI.Network.RestApi;
using PQBI.Network.RestApi.EngineCalculation;
using PQBI.Network.RestApi.Validations;
using PQBI.PQS;
using PQBI.Schemas;
using PQBI.Trace;
using PQBI.Web.Chat.SignalR;
using PQBI.Web.Common;
using PQBI.Web.HealthCheck;
using PQBI.Web.Infrastructures;
using PQBI.Web.Middlewares;
using PQBI.Web.Models;
using PQBI.Web.MultiTenancy;
using PQBI.Web.OpenIddict;
using PQBI.Web.Swagger;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Settings.Configuration;
using Stripe;
using System.Diagnostics;
using System.Net.Security;
using System.Reflection;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using HealthChecksUISettings = HealthChecks.UI.Configuration.Settings;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

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

            bool isRunningInContainer = IsRunningInContainer();
            //Log.Error($"isRunningInContainer: {isRunningInContainer}");

            _appConfiguration = env.GetAppConfiguration();

            //if (isRunningInContainer)
            //    _appConfiguration = env.GetAppConfiguration();
            //else
            //    _appConfiguration = BuildConfiguration(env);


            var pd = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "PQBI", "backend", "Configurations"
);

            //Log.Information("CONFIG: env={Env} contentRoot={Root}", env.EnvironmentName, env.ContentRootPath);
            //Log.Information("CONFIG: pdDir={Pd} exists={Exists}", pd, Directory.Exists(pd));
            //Log.Information("CONFIG: pdProdExists={Exists}",
            //    );

            //Log.Information("CONFIG: Default CS = {CS}",
            //    _appConfiguration.GetConnectionString("Default"));

        }

        private static bool IsRunningInContainer()
        {
            // 1) Your explicit switch (set by Docker / setup)
            //    Values treated as true: "1", "true", "yes", "on"
            var v = Environment.GetEnvironmentVariable("PQBI_RUN_IN_CONTAINER");
            if (!string.IsNullOrWhiteSpace(v))
                return IsTrue(v);

            // 2) Fallback: .NET sets this in containers
            var dotnet = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            if (!string.IsNullOrWhiteSpace(dotnet))
                return IsTrue(dotnet);

            return false;
        }

        private static bool IsTrue(string s)
        {
            s = s.Trim();
            return s.Equals("1", StringComparison.OrdinalIgnoreCase)
                || s.Equals("true", StringComparison.OrdinalIgnoreCase)
                || s.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || s.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        private static IConfigurationRoot BuildConfiguration(IWebHostEnvironment env)
        {
            // Pick your folder. Example:
            // C:\ProgramData\Elspec\PQBI\config
            var programDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var programDataConfigDir = Path.Combine(programDataRoot, "PQBI", "backend", "Configurations");

            var envName = env.EnvironmentName; // e.g. "Production", "Staging", "Development"

            if (!Directory.Exists(programDataConfigDir))
                Directory.CreateDirectory(programDataConfigDir);


            //Log.Information("BuildConfiguration CONFIG: env={Env} contentRoot={Root}", env.EnvironmentName, env.ContentRootPath);
            //Log.Information("BuildConfiguration CONFIG: pdDir={Pd} exists={Exists}", programDataConfigDir, Directory.Exists(programDataConfigDir));
            //Log.Information("BuildConfiguration CONFIG: pdProdExists={Exists}",
            //    System.IO.File.Exists(Path.Combine(programDataConfigDir, $"appsettings.{env.EnvironmentName}.json")));
  
            //Log.Error("BuildConfiguration");

            var pdProvider = new PhysicalFileProvider(programDataConfigDir);

            // Note: Later providers override earlier ones.
            return new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)

                // 1) Defaults shipped with the app (Program Files / site folder)
                //.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                //.AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: true)

                // 2) Customer overrides (ProgramData) — only if they exist
                .AddJsonFile(pdProvider, "appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(pdProvider, $"appsettings.{envName}.json", optional: true, reloadOnChange: true)

                // Keep these (so IIS env vars etc still work)
                .AddEnvironmentVariables()
                .Build();
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

            // Configure CORS: allow requests from ANY origin (including credentials)
            // ?? Security note: this trusts any website to send authenticated requests.
            // Use only if you understand the risk or you're on a trusted network.
            //services.AddCors(options =>
            //{
            //    options.AddPolicy(DefaultCorsPolicyName, builder =>
            //    {
            //        builder
            //            .SetIsOriginAllowed(_ => true) // accept ALL origins dynamically
            //            .AllowAnyHeader()
            //            .AllowAnyMethod()
            //            .AllowCredentials();           // cookies/bearer in xhr/fetch allowed
            //    });
            //});

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

            WeekConfiguration.Configure(_appConfiguration);

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
            services.AddTransient<IFeederChannelTredBuilder, FeederChannelTredBuilder>();

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

            string isRunningInsideDockerStr = variableWriter.PQBI_RUN_IN_CONTAINER ?? _appConfiguration["App:IsRunningInsideDocker"];
            bool isRunningInsideDocker = false;
            if (!string.IsNullOrEmpty(isRunningInsideDockerStr))
            {
                isRunningInsideDocker = IsTrue(isRunningInsideDockerStr);
            }


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
                var seqHost = variableWriter.SEQ_HOST_URL ?? seqConfig.Url;

                var isWindows = OperatingSystem.IsWindows();

                var cfg = new LoggerConfiguration()
                            .MinimumLevel.Information()
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                            .ReadFrom.Configuration(_appConfiguration, new ConfigurationReaderOptions
                             {
                                 SectionName = "Serilog"
                             })
                            //.ReadFrom.Configuration(_appConfiguration)   // if Serilog section exists, it overrides
                            .Enrich.FromLogContext();

                //var cfg = new LoggerConfiguration()
                //    .Enrich.FromLogContext()
                //    .MinimumLevel.Information()
                //    // keep noise down from framework
                //    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                //    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning);

                if (isRunningInsideDocker)
                {
                    // containers: stdout
                    cfg = cfg.WriteTo.Console();
                }
                else
                {
                    // Outside docker:
                    // - On Windows: log to ProgramData + EventLog
                    // - On non-Windows: usually log to files under app dir or /var/log (your choice)
                    string baseDir;

                    if (isWindows)
                    {
                        // default ProgramData\PQBI\Logs unless overridden by logDir

                        baseDir = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                "PQBI", "backend", "Logs");

                        //baseDir = string.IsNullOrWhiteSpace(logDir)
                        //    ? Path.Combine(
                        //        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        //        "PQBI", "Logs")
                        //    : logDir;
                    }
                    else
                    {
                        // Non-Windows fallback (pick what fits your deployment)
                        // You can also just do Console here if you prefer.

                        baseDir = Path.Combine(AppContext.BaseDirectory, "Logs");

                        //baseDir = string.IsNullOrWhiteSpace(logDir)
                        //    ? Path.Combine(AppContext.BaseDirectory, "Logs")
                        //    : logDir;
                    }

                    if (Directory.Exists(baseDir) == false)
                        Directory.CreateDirectory(baseDir);

                    cfg = cfg
                        .WriteTo.File(
                            Path.Combine(baseDir, "log.txt"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30,
                            shared: true)
                        .WriteTo.File(
                            new JsonFormatter(),
                            Path.Combine(baseDir, "log.json"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30,
                            shared: true);

                    // Event Viewer: ONLY on Windows and only Error/Fatal
                    // Requires:
                    // 1) Serilog.Sinks.EventLog package
                    // 2) Installer creates custom log "PQBI" and registers source "PQBI.Web.Host" under it
                    // 3) manageEventSource=false so runtime won't try to create registry keys
                    if (isWindows)
                    {
                        const string eventSource = "PQBI.Web.Host"; // Source column
                                                                    // log name is not passed here; it is determined by the source registration.
                                                                    // You must register this source under log "PQBI" during install.

                        bool isEventSourceExist = TrySourceExists(eventSource);
                        if (isEventSourceExist)
                        {
                            cfg = cfg.ReadFrom.Configuration(_appConfiguration, new ConfigurationReaderOptions
                            {
                                SectionName = "SerilogEventLog"
                            });
                        }

                        //cfg = cfg.WriteTo.Logger(lc => lc
                        //    .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
                        //    .WriteTo.EventLog(
                        //        source: eventSource,
                        //        manageEventSource: false
                        //    ));
                    }
                }

                if (!string.IsNullOrWhiteSpace(seqHost))
                    cfg = cfg.WriteTo.Seq(seqHost);

                Log.Logger = cfg.CreateLogger();

                options.IocManager.IocContainer.AddFacility<LoggingFacility>(
                    f => f.LogUsing(new AdapterSerilogFactory(Log.Logger)));

                options.PlugInSources.AddFolder(
                    Path.Combine(_hostingEnvironment.WebRootPath, "Plugins"),
                    SearchOption.AllDirectories);

                //variableWriter.WriteAllVaribles();
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

            Log.Error("Configure");
           


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

        public static bool TrySourceExists(string source)
        {
            try
            {
                return EventLog.SourceExists(source);
            }
            catch (SecurityException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
    }
}
