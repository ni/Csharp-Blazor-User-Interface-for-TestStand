using Microsoft.AspNetCore.Components.Server.Circuits;
using NationalInstruments.TestStand.WebOI.UI.Services;
using NationalInstruments.TestStand.WebOI.Utilities;
using Serilog;

namespace NationalInstruments.TestStand.WebOI.WebServer
{
    internal sealed class Program
    {
        private static readonly string _urlFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "National Instruments",
            "TestStand WebOI",
            "webserver-url.txt");

        public static async Task Main(string[] arguments)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(arguments);

            builder.Host.ConfigureSerilogLogging();

            _ = builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            _ = builder.Services.AddSequencingServices();
            _ = builder.Services.AddScopedServices();
            _ = builder.Services.AddSingleton<CircuitHandler, ConnectionCircuitHandler>();
            WebApplication app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                _ = app.UseHsts();
            }

            _ = app.UseHttpsRedirection();
            _ = app.UseStaticFiles();
            _ = app.UseAntiforgery();
            _ = app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            await app.StartAsync();
            try
            {
                string? urlFileDirectory = Path.GetDirectoryName(_urlFilePath);
                if (urlFileDirectory is not null)
                {
                    _ = Directory.CreateDirectory(urlFileDirectory);
                }
                string? url = app.Urls.FirstOrDefault();
                string? address = url is null ? null : new UriBuilder(url) { Host = "localhost" }.Uri?.ToString();
                await File.WriteAllTextAsync(_urlFilePath, address);
                Console.WriteLine($"Web server started. Url: {address}");
                await app.WaitForShutdownAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to write URL file: {FilePath}", _urlFilePath);
                await app.StopAsync();
            }

            try
            {
                if (File.Exists(_urlFilePath))
                {
                    File.Delete(_urlFilePath);
                }
            }
            catch
            {
                // Ignore exceptions on shutdown
                Log.Logger.Warning("Failed to delete URL file on shutdown: {FilePath}", _urlFilePath);
            }

            await Log.CloseAndFlushAsync();
        }
    }
}
