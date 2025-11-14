using System;

namespace PQBI.Network.RestApi
{
    public static class ConfigurationVersionProvider
    {
        private static int _currentVersion = 1; // Initial value
        private static readonly object _lock = new();

        public static int GetCurrentVersion()
        {
            lock (_lock)
            {
                return _currentVersion;
            }
        }

        public static void UpdateVersion()
        {           
            lock (_lock)
            {
                _currentVersion++;
            }
        }
    }
}
