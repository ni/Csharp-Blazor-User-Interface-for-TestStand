namespace NationalInstruments.TestStand.WebOI.UI.Services.Events
{
    /// <summary>
    /// Encapsulates the information for the InvokeInfoBanner event.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="InvokeInfoBannerEventArgs"/> class.
    /// </remarks>
    /// <param name="message">The information message to display.</param>
    /// <param name="filePath">Optional file path to display with a copy button.</param>
    internal sealed class InvokeInfoBannerEventArgs(string message, string? filePath = null) : EventArgs
    {
        /// <summary>
        /// Gets the information message to display.
        /// </summary>
        public string Message { get; } = message;

        /// <summary>
        /// Gets the optional file path to display.
        /// </summary>
        public string? FilePath { get; } = filePath;
    }
}