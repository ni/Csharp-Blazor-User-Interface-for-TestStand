using Grpc.Core;

namespace NationalInstruments.TestStand.WebOI.UI.Services.Events
{
    /// <summary>
    /// Event arguments for handling <see cref="RpcException"/>s.
    /// </summary>
    /// <param name="exception">The <see cref="RpcException"/> that occurred.</param>
    internal sealed class RpcErrorEventArgs(RpcException exception) : EventArgs
    {
        internal const string GeneralErrorMessage = "Error. Find more details in the logs in 'ProgramData/National Instruments/TestStand/Logs'.";

        /// <summary>
        /// A user-facing error message.
        /// </summary>
        public string Message { get; } = exception switch
        {
            RpcException e when ExceptionIndicatesServiceUnavailable(e) => "TestStand WebOI failed. Please try restarting the machine, and if the problem persists visit ni.com to request support.",
            _ when !string.IsNullOrEmpty(exception.Status.Detail) => exception.Status.Detail,
            _ => GeneralErrorMessage
        };

        private static bool ExceptionIndicatesServiceUnavailable(RpcException exception)
        {
            return exception.StatusCode is StatusCode.Unavailable
                || (exception.StatusCode is StatusCode.Unknown && exception.Message.Contains("Stream removed", StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
