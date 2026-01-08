using NationalInstruments.Sequencing.V2;

namespace NationalInstruments.TestStand.BlazorOI.UI.Services.Events
{
    // Encapsulates information about the current execution timing information.
    internal sealed class ExecutionTimeInfoEventArgs(int executionId, ExecutionTimeInfo executionTimeInfo) : EventArgs
    {
        public int ExecutionId { get; } = executionId;
        public ExecutionTimeInfo ExecutionTimeInfo { get; } = executionTimeInfo;
    }
}