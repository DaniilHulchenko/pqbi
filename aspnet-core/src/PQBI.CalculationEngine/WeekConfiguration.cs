using Microsoft.Extensions.Configuration;
using System;

namespace PQBI.Configuration
{
    public static class WeekConfiguration
    {
        private static DayOfWeek? _startOfWeek;
        private static bool _isConfigured = false;

        /// <summary>
        /// Gets the configured start of week. Returns Monday if configured as Monday start,
        /// otherwise returns Sunday (default).
        /// </summary>
        public static DayOfWeek StartOfWeek
        {
            get
            {
                if (_startOfWeek.HasValue)
                    return _startOfWeek.Value;

                // If not explicitly configured via Configure() method, try environment variable
                if (!_isConfigured)
                {
                    var envValue = Environment.GetEnvironmentVariable("WEEK_START_DAY");
                    if (!string.IsNullOrWhiteSpace(envValue))
                    {
                        if (Enum.TryParse<DayOfWeek>(envValue, true, out var dayOfWeek))
                        {
                            _startOfWeek = dayOfWeek;
                            return _startOfWeek.Value;
                        }
                    }
                }

                // Fall back to default
                _startOfWeek = DayOfWeek.Sunday;
                return _startOfWeek.Value;
            }
        }

        /// <summary>
        /// Returns true if Monday is the start of the week.
        /// </summary>
        public static bool IsMondayStartOfWeek => StartOfWeek == DayOfWeek.Monday;

        /// <summary>
        /// Allows explicit override of the start of week (useful for testing or runtime changes).
        /// </summary>
        public static void SetStartOfWeek(DayOfWeek dayOfWeek)
        {
            _startOfWeek = dayOfWeek;
            _isConfigured = true;
        }

        /// <summary>
        /// Reset to default behavior (re-read from environment/config).
        /// </summary>
        public static void Reset()
        {
            _startOfWeek = null;
            _isConfigured = false;
        }

        /// <summary>
        /// Configure the start of the week from the application configuration.
        /// Call this in Startup.Configure() to set from appsettings.json.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        public static void Configure(IConfiguration configuration)
        {
            var configuredValue = configuration["App:WeekStartDay"];
            if (!string.IsNullOrEmpty(configuredValue))
            {
                if (Enum.TryParse<DayOfWeek>(configuredValue, true, out var dayOfWeek))
                {
                    _startOfWeek = dayOfWeek;
                    _isConfigured = true;
                    return;
                }
            }

            // If no valid configuration in appsettings, try environment variable
            var envValue = Environment.GetEnvironmentVariable("WEEK_START_DAY");
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                if (Enum.TryParse<DayOfWeek>(envValue, true, out var dayOfWeek))
                {
                    _startOfWeek = dayOfWeek;
                    _isConfigured = true;
                }
            }
        }
    }
}