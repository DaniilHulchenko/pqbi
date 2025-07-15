using Newtonsoft.Json;
using PQBI.Infrastructure;
using System;
using System.Collections.Generic;

namespace PQBI.Configuration
{
    public interface IPQSConfigurationService
    {
        void AddConfig<TConfig>(TConfig config) where TConfig : PQSConfig<TConfig>;
        string GetAllConfiguration();
    }

    public class PQSConfigurationService : IPQSConfigurationService
    {
        private Dictionary<string, string> _configurations;

        public PQSConfigurationService()
        {
            _configurations = new Dictionary<string, string>();
        }

        public void AddConfig<TConfig>(TConfig config) where TConfig : PQSConfig<TConfig>
        {
            try
            {
                var tmp = JsonConvert.SerializeObject(config);
                _configurations[config.ConfigurationName] = JsonConvert.SerializeObject(config);

            }
            catch (Exception e)
            {
                //_logger.LogWarning("PQSConfigurationManager Failed to serialize", e);
            }
        }


        public string GetAllConfiguration()
        {
            var tmp =  JsonConvert.SerializeObject(_configurations);
            var tmp2 = tmp.Replace("\\","");

            return tmp2;
        }
    }
}
