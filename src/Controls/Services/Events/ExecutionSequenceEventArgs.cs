using NationalInstruments.TestStand.WebOI.SharedDomain.Models;

namespace NationalInstruments.TestStand.WebOI.UI.Services.Events
{
    internal sealed class ExecutionSequenceEventArgs(int executionId, Sequence executionSequence) : EventArgs
    {
        public Sequence ExecutionSequence { get; } = executionSequence;
        public int ExecutionId { get; } = executionId;
    }
}