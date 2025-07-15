using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PQBI.Configuration;
using PQBI.Web;
using System;
using System.Collections.Generic;

namespace PQBI.EntityFrameworkCore
{
    /* This class is needed to run "dotnet ef ..." commands from command line on development. Not used anywhere else */
    public class PQBIDbContextFactory : IDesignTimeDbContextFactory<PQBIDbContext>
    {
        const string Env_Type_Key = "env";
        const string Connection_String_Key = "connection";

        public PQBIDbContext CreateDbContext(string[] args)
        {

            var arguments = Parse(args);
            var builder = new DbContextOptionsBuilder<PQBIDbContext>();


            string env = null;

            arguments.TryGetValue(Env_Type_Key, out env);

            /*
             You can provide an environmentName parameter to the AppConfigurations.Get method. 
             In this case, AppConfigurations will try to read appsettings.{environmentName}.json.
             Use Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") method or from string[] args to get environment if necessary.
             https://docs.microsoft.com/en-us/ef/core/cli/dbcontext-creation?tabs=dotnet-core-cli#args
             */
            var configuration = AppConfigurations.Get(
                WebContentDirectoryFinder.CalculateContentRootFolder(),
                environmentName: env,
                addUserSecrets: true
            );


            var pqbiSection = configuration.GetSection(PqbiConfig.ApiName);
            var pqbiConfig = pqbiSection.Get<PqbiConfig>();


            var currentConnectionString = configuration.GetConnectionString(PQBIConsts.ConnectionStringName);


            if (pqbiConfig.MultiTenancyEnabled == false)
            {
                var tmp = configuration.GetConnectionString("SQLiteDb");
                if(string.IsNullOrEmpty(tmp) == false)
                {
                    currentConnectionString = tmp;
                }
            }
            else
            {
                var tmp = configuration.GetConnectionString("PostgresDb");
                if (string.IsNullOrEmpty(tmp) == false)
                {
                    currentConnectionString = tmp;
                }
            }

            if(arguments.TryGetValue(Connection_String_Key,out var con))
            {
                currentConnectionString = con;
            }


            Console.WriteLine($"---------- Connection string = {currentConnectionString}");
            PQBIDbContextConfigurer.Configure(builder, currentConnectionString, pqbiConfig.MultiTenancyEnabled);

            return new PQBIDbContext(builder.Options, Options.Create(pqbiConfig));
        }


        private IDictionary<string, string> Parse(string[] args)
        {
            var arguments = new Dictionary<string, string>();

            if (args != null && args.Length > 0)
            {
                int i = 1;
                Console.WriteLine("The arguments are:");
                foreach (var arg in args)
                {
                    var parameters = arg.Split(':');
                    var key = parameters[0];
                    var val = parameters[1];

                    arguments[key] = val;

                    Console.WriteLine($"{i++}. {key}:{val}");
                }
            }

            return arguments;
        }
    }
}
