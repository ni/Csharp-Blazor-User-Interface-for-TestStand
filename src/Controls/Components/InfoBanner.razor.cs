using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NationalInstruments.TestStand.BlazorOI.UI.Services;
using NationalInstruments.TestStand.BlazorOI.UI.Services.Events;

namespace NationalInstruments.TestStand.BlazorOI.UI.Components
{
    /// <summary>
    /// Represents a UI component that displays information messages with optional file path and copy functionality.
    /// </summary>
    public sealed partial class InfoBanner : ComponentBase, IDisposable
    {
        [Inject]
        private IAppStateService AppStateService { get; set; } = null!;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;

        [Inject]
        private ILogger<InfoBanner> Logger { get; set; } = null!;

        private bool Open { get; set; }
        private string Message { get; set; } = string.Empty;
        private string FilePath { get; set; } = string.Empty;

        private EventHandler<InvokeInfoBannerEventArgs>? _onInvokeInfoBannerHandler;

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            _onInvokeInfoBannerHandler = async (s, e) => await HandleInvokeInfoBannerEventAsync(e);
            AppStateService.OnInvokeInfoBanner += _onInvokeInfoBannerHandler;
        }

        private async Task HandleInvokeInfoBannerEventAsync(InvokeInfoBannerEventArgs e)
        {
            Open = true;
            Message = e.Message;
            FilePath = e.FilePath ?? string.Empty;
            await InvokeAsync(StateHasChanged);
        }

        private async Task CopyPathToClipboardAsync()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", FilePath);
                Logger.LogInformation("Path copied to clipboard: {FilePath}", FilePath);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to copy path to clipboard: {Message}", ex.Message);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_onInvokeInfoBannerHandler != null)
            {
                AppStateService.OnInvokeInfoBanner -= _onInvokeInfoBannerHandler;
                _onInvokeInfoBannerHandler = null;
            }
        }
    }
}