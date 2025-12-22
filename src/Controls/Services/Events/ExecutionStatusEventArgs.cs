using NationalInstruments.Sequencing.V2;

namespace NationalInstruments.TestStand.WebOI.UI.Services.Events
{
    // Encapsulates information about the current execution status.
    internal sealed class ExecutionStatusEventArgs(int executionId, ExecutionStatus status) : EventArgs
    {
        public ExecutionStatus Status { get; } = status;
        public int ExecutionId { get; } = executionId;
    }
}