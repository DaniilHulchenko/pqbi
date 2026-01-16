using Abp.Extensions;
using Abp.Reflection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace PQBI.Configuration
{
    public static class AppConfigurations
    {
        private static readonly ConcurrentDictionary<string, IConfigurationRoot> ConfigurationCache;

        static AppConfigurations()
        {
            ConfigurationCache = new ConcurrentDictionary<string, IConfigurationRoot>();
        }

        public static IConfigurationRoot Get(string path, string environmentName = null, bool addUserSecrets = false)
        {
            var cacheKey = path + "#" + environmentName + "#" + addUserSecrets;
            return ConfigurationCache.GetOrAdd(
                cacheKey,
                _ => BuildConfiguration(path, environmentName, addUserSecrets)
            );
        }

        private static IConfigurationRoot BuildConfiguration(string path, string environmentName = null,
            bool addUserSecrets = false)
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(path)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

            bool isDebug =
#if DEBUG
   true;
#else
    false;
#endif

            if (!isDebug)
            {
                // Add ProgramData overrides ONLY outside containers
                if (!IsRunningInContainer())
                {
                    var pdDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "PQBI", "backend", "Configurations"
                    );

                    Directory.CreateDirectory(pdDir);

                    var pdProvider = new PhysicalFileProvider(pdDir);

                    builder
                        .AddJsonFile(pdProvider, "appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile(pdProvider, $"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);
                }
            }

            //var builder = new ConfigurationBuilder()
            //    .SetBasePath(path)
            //    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            //if (!environmentName.IsNullOrWhiteSpace())
            //{
            //    builder = builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
            //}

            builder = builder.AddEnvironmentVariables();

            if (addUserSecrets)
            {
                builder.AddUserSecrets(typeof(AppConfigurations).GetAssembly(), true);
            }

            var builtConfig = builder.Build();
            new AppAzureKeyVaultConfigurer().Configure(builder, builtConfig);

            return builder.Build();
        }

        private static bool IsRunningInContainer()
        {
            var v = Environment.GetEnvironmentVariable("PQBI_RUN_IN_CONTAINER");
            if (!string.IsNullOrWhiteSpace(v))
                return IsTrue(v);

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
    }
}
