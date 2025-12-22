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

        event EventHandler<bool>? OnTogglePane;

        event EventHandler<InvokeErrorBannerEventArgs>? OnInvokeErrorBanner;
    }
}
