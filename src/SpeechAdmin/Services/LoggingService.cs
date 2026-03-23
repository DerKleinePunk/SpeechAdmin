using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SpeechAdmin.Configuration;

namespace SpeechAdmin.Services
{
    /// <summary>
    /// Service for configuring application logging
    /// </summary>
    public static class LoggingService
    {
        /// <summary>
        /// Adds custom logging configuration to the service collection
        /// </summary>
        public static IServiceCollection AddCustomLogging(this IServiceCollection services, AppSettings appSettings)
        {
            // Configure Serilog
            var loggerConfiguration = new LoggerConfiguration();

            // Set minimum level from configuration
            var defaultLevelString = appSettings.Logging.LogLevel.TryGetValue("Default", out var levelStr) ? levelStr : "Information";
            var defaultLevel = ParseLogLevel(defaultLevelString);
            loggerConfiguration.MinimumLevel.Is(defaultLevel);

            // Configure level overrides
            foreach (var kvp in appSettings.Logging.LogLevel)
            {
                if (kvp.Key != "Default")
                {
                    var logEventLevel = ParseLogLevel(kvp.Value);
                    loggerConfiguration.MinimumLevel.Override(kvp.Key, logEventLevel);
                }
            }

            // Add File sink if enabled
            if (appSettings.Logging.File.Enabled)
            {
                var rollingInterval = ParseRollingInterval(appSettings.Logging.File.RollingInterval);

                loggerConfiguration.WriteTo.File(
                    path: appSettings.Logging.File.Path,
                    rollingInterval: rollingInterval,
                    retainedFileCountLimit: appSettings.Logging.File.RetainedFileCountLimit,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
            }

            // Create Serilog logger
            Log.Logger = loggerConfiguration.CreateLogger();

            // Add Serilog to Microsoft.Extensions.Logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });

            return services;
        }

        private static LogEventLevel ParseLogLevel(string level)
        {
            return level switch
            {
                "Trace" => LogEventLevel.Verbose,
                "Debug" => LogEventLevel.Debug,
                "Information" => LogEventLevel.Information,
                "Warning" => LogEventLevel.Warning,
                "Error" => LogEventLevel.Error,
                "Critical" => LogEventLevel.Fatal,
                "None" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }

        private static RollingInterval ParseRollingInterval(string interval)
        {
            return interval switch
            {
                "Infinite" => RollingInterval.Infinite,
                "Year" => RollingInterval.Year,
                "Month" => RollingInterval.Month,
                "Day" => RollingInterval.Day,
                "Hour" => RollingInterval.Hour,
                "Minute" => RollingInterval.Minute,
                _ => RollingInterval.Day
            };
        }
    }
}