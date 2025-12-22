using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace NationalInstruments.TestStand.WebOI.WebServer
{
    /// <summary>
    /// Derives from <see cref="ErrorBoundary"/> to log all unhandled exceptions, and intentionally does not
    /// render anything custom when an error occurs.
    /// </summary>
    public sealed partial class LoggingErrorBoundary : ErrorBoundary
    {
        [Inject]
        private ILogger<LoggingErrorBoundary> Logger { get; set; } = null!;

        /// <inheritdoc />
        protected override Task OnErrorAsync(Exception exception)
        {
            Logger.LogError("Unhandled exception. Message: {Message}, Call stack: {CallStack}", exception.Message, exception.StackTrace);
            return base.OnErrorAsync(exception);
        }
    }
}
