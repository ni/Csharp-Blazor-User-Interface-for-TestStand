using NationalInstruments.TestStand.BlazorOI.SharedDomain.Models;

namespace NationalInstruments.TestStand.BlazorOI.UI.Services.Events
{
    internal sealed class ExecutionSequenceEventArgs(int executionId, Sequence executionSequence) : EventArgs
    {
        public Sequence ExecutionSequence { get; } = executionSequence;
        public int ExecutionId { get; } = executionId;
    }
}