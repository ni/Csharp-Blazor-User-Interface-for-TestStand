using Microsoft.AspNetCore.Components.Server.Circuits;
using NationalInstruments.TestStand.BlazorOI.UI.Services;

namespace NationalInstruments.TestStand.BlazorOI.WebServer
{
    internal sealed class ConnectionCircuitHandler(
        IServiceProvider serviceProvider) : CircuitHandler
    {
        private readonly HashSet<Circuit> circuits = [];
        private bool _isConnectedToSequencingService;

        /// <inheritdoc/>
        public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            try
            {
                _ = circuits.Add(circuit);
                if (!_isConnectedToSequencingService)
                {
                    await serviceProvider.ConnectSequencingServicesAsync();
                    _isConnectedToSequencingService = true;
                }
            }
            catch (Exception)
            {
                // do not throw from this handler as suggested by MS
            }
        }

        /// <inheritdoc/>
        public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
        {
            try
            {
                _ = circuits.Remove(circuit);
                if (circuits.Count == 0 && _isConnectedToSequencingService)
                {
                    serviceProvider.DisconnectSequencingServices();
                    _isConnectedToSequencingService = false;
                }
            }
            catch (Exception)
            {
                // do not throw from this handler as suggested by MS
            }
            return Task.CompletedTask;
        }
    }
}
