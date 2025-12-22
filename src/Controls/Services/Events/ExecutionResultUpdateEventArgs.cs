namespace NationalInstruments.TestStand.WebOI.UI.Services.Events
{
    internal sealed class ExecutionResultUpdateEventArgs(int executionId, string executionResultStatus) : EventArgs
    {
        public string ExecutionResult { get; } = executionResultStatus;
        public int ExecutionId { get; } = executionId;
    }
}