using System.Globalization;
using NationalInstruments.MeasurementLink.Discovery.V1;
using NationalInstruments.TestStand.WebOI.Utilities.DevelopmentFeatures;
using NationalInstruments.TestStand.WebOI.Utilities.Discovery;
using Serilog;
using Serilog.Events;

namespace NationalInstruments.TestStand.WebOI.Utilities
{
    /// <summary>
    /// Utility methods for working with an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services related to discovery to the given <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>A reference to the service collection with the discovery services added.</returns>
        public static IServiceCollection AddDiscoveryServices(this IServiceCollection services)
        {
            _ = services.AddSingleton<IDiscoveryClient, DiscoveryClient>();
            return services.AddSingleton<IDiscoveryServiceUtility, DiscoveryServiceUtility>();
        }

        /// <summary>
        /// Adds services related to development features to the given <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>A reference to the service collection with the development features services added.</returns>
        public static IServiceCollection AddDevelopmentFeaturesServices(this IServiceCollection services)
        {
            return services.AddSingleton<IDevelopmentFeaturesService, DevelopmentFeaturesService>();
        }

        /// <summary>
        /// Set up logging with Serilog. This will use the Serilog configuration defined in the appsettings.json that has been loaded for the
        /// host, and add an Async Sink for the Console. The Console logger will write Information or higher events when the beacon file is present,
        /// but only log Fatal events otherwise.
        /// </summary>
        /// <param name="host">Host builder from a ASP.NET Core application.</param>
        public static void ConfigureSerilogLogging(this ConfigureHostBuilder host)
        {
            LogEventLevel consoleLogEventLevel = File.Exists(DevelopmentFeaturesService.ShowDevelopmentFeaturesFileName)
                ? LogEventLevel.Information
                : LogEventLevel.Fatal;
            _ = host.UseSerilog(
                (hostContext, logger) => logger.ReadFrom.Configuration(hostContext.Configuration)
                .WriteTo.Async(x => x.Console(consoleLogEventLevel, formatProvider: CultureInfo.InvariantCulture)),
                writeToProviders: false);
        }
    }
}
