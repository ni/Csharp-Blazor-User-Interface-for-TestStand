using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NationalInstruments.TestStand.WebOI.Utilities;

namespace NationalInstruments.TestStand.WebOI.UI.Services
{
    /// <summary>
    /// Extensions for working with services required by the TestStand WebOI UI components.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add services required for sequencing to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">Services collection.</param>
        /// <returns>The service collection with required sequencing services added.</returns>
        public static IServiceCollection AddSequencingServices(this IServiceCollection services)
        {
            _ = services.AddSingleton<IFileSystem, FileSystem>();
            _ = services.AddSequencingServiceClient();
            _ = services.AddDevelopmentFeaturesServices();
            _ = services.AddDiscoveryServices();
            _ = services.AddSingleton<ThemeProvider>();
            return services;
        }

        /// <summary>
        /// Add scoped services required by the TestStand WebOI UI components to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection to which the scoped services will be added.</param>
        /// <returns>The service collection with the scoped services added.</returns>
        public static IServiceCollection AddScopedServices(this IServiceCollection services)
        {
            _ = services.AddScoped<IUserStateService, UserStateService>();
            _ = services.AddScoped<IAppStateService, AppStateService>();
            _ = services.AddScoped<ISequenceFileStateService, SequenceFileStateService>();
            _ = services.AddScoped<IExecutionStateService, ExecutionStateService>();
            _ = services.AddScoped<ISequencingObserver, SequencingObserver>();
            return services;
        }

        private static IServiceCollection AddSequencingServiceClient(this IServiceCollection services)
        {
            _ = services.AddSingleton<SequencingServiceClient>();
            _ = services.AddSingleton<ISequencingServiceClient>(s => s.GetRequiredService<SequencingServiceClient>());
            _ = services.AddSingleton<IUserManagementClient>(s => s.GetRequiredService<SequencingServiceClient>());
            _ = services.AddSingleton<ISequencingServiceClientConnection>(s => s.GetRequiredService<SequencingServiceClient>());
            return services;
        }

        /// <summary>
        /// Connect to the gRPC Sequencing Services.
        /// </summary>
        /// <param name="serviceProvider">Service provider that has the <see cref="ISequencingServiceClientConnection"/> registered.</param>
        public static async Task ConnectSequencingServicesAsync(this IServiceProvider serviceProvider)
        {
            ISequencingServiceClientConnection client = serviceProvider.GetRequiredService<ISequencingServiceClientConnection>();
            await client.ConnectServicesAsync();
        }

        /// <summary>
        /// Disconnect from the gRPC Sequencing Services.
        /// </summary>
        /// <param name="serviceProvider">Service provider that has the <see cref="ISequencingServiceClientConnection"/> registered.</param>
        public static void DisconnectSequencingServices(this IServiceProvider serviceProvider)
        {
            ISequencingServiceClientConnection client = serviceProvider.GetRequiredService<ISequencingServiceClientConnection>();
            client.DisconnectServices();
        }
    }
}
