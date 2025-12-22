using Microsoft.AspNetCore.Components;
using NationalInstruments.TestStand.WebOI.UI.Services;
using NationalInstruments.TestStand.WebOI.UI.Services.Events;

namespace NationalInstruments.TestStand.WebOI.UI.Components
{
    /// <summary>
    /// Represents a UI component that displays error messages triggered by RPC errors.
    /// </summary>
    /// <remarks>The <see cref="ErrorBanner"/> component listens for RPC error events from the injected  <see
    /// cref="ISequencingServiceClient"/> and displays an error message when such events occur.</remarks>
    public sealed partial class ErrorBanner : ComponentBase, IDisposable
    {
        [Inject]
        private ISequencingServiceClient SequencingClient { get; set; } = null!;

        [Inject]
        private IUserManagementClient UserManagementClient { get; set; } = null!;

        [Inject]
        private ISequenceFileStateService SequenceFileStateService { get; set; } = null!;

        [Inject]
        private IAppStateService AppStateService { get; set; } = null!;

        private bool Open { get; set; }
        private string Message { get; set; } = string.Empty;

        private EventHandler<RpcErrorEventArgs>? _errorHandler;
        private EventHandler<InvokeErrorBannerEventArgs>? _onInvokeErrorBannerHandler;

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            _errorHandler = async (s, e) => await HandleRpcErrorAsync(e);
            _onInvokeErrorBannerHandler = async (s, e) => await HandleInvokeErrorBannerEventAsync(e);
            SequencingClient.RpcError += _errorHandler;
            UserManagementClient.RpcError += _errorHandler;
            SequenceFileStateService.OnInvokeErrorBanner += _onInvokeErrorBannerHandler;
            AppStateService.OnInvokeErrorBanner += _onInvokeErrorBannerHandler;
        }

        private async Task HandleRpcErrorAsync(RpcErrorEventArgs e)
        {
            Open = true;
            Message = e.Message;
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleInvokeErrorBannerEventAsync(InvokeErrorBannerEventArgs e)
        {
            Open = true;
            Message = e.Message;
            await InvokeAsync(StateHasChanged);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_errorHandler != null)
            {
                SequencingClient.RpcError -= _errorHandler;
                UserManagementClient.RpcError -= _errorHandler;
                _errorHandler = null;
            }
            if (_onInvokeErrorBannerHandler != null)
            {
                SequenceFileStateService.OnInvokeErrorBanner -= _onInvokeErrorBannerHandler;
                AppStateService.OnInvokeErrorBanner -= _onInvokeErrorBannerHandler;
                _onInvokeErrorBannerHandler = null;
            }
        }
    }
}
