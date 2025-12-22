using NationalInstruments.TestStand.WebOI.UI.Services.Events;

namespace NationalInstruments.TestStand.WebOI.UI.Services
{
    internal sealed class AppStateService : IAppStateService
    {
        private bool _isSequencePaneOpen = true;

        public event EventHandler<bool>? OnTogglePane;
        public event EventHandler<InvokeErrorBannerEventArgs>? OnInvokeErrorBanner;

        public bool IsSequencePaneOpen
        {
            get => _isSequencePaneOpen;
            set
            {
                _isSequencePaneOpen = value;
                OnTogglePane?.Invoke(this, value);
            }
        }

        public void InvokeErrorBanner(string message)
        {
            OnInvokeErrorBanner?.Invoke(this, new InvokeErrorBannerEventArgs(message));
        }
    }
}
