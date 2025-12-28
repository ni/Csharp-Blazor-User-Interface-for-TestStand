using NationalInstruments.TestStand.WebOI.UI.Services.Events;

namespace NationalInstruments.TestStand.WebOI.UI.Services
{
    /// <summary>
    /// Service for handling the application state.
    /// </summary>
    internal interface IAppStateService
    {
        bool IsSequencePaneOpen { get; set; }

        void InvokeErrorBanner(string message);
        void InvokeInfoBanner(string message, string? filePath = null);

        event EventHandler<bool>? OnTogglePane;

        event EventHandler<InvokeErrorBannerEventArgs>? OnInvokeErrorBanner;
        event EventHandler<InvokeInfoBannerEventArgs>? OnInvokeInfoBanner;
    }
}
