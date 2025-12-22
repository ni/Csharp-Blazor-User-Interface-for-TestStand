namespace NationalInstruments.TestStand.WebOI.UI.Services.Events
{
    // Encapsulates the error information for the InvokeErrorBanner event.
    internal sealed class InvokeErrorBannerEventArgs(string message) : EventArgs
    {
        public string Message { get; } = message;
    }
}