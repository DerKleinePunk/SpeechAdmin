using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        public static IServiceCollection AddCustomLogging(this IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
#if DEBUG
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddDebug();
#else
                builder.SetMinimumLevel(LogLevel.Information);
#endif
                builder.AddConsole();
            });

            return services;
        }
    }
}
