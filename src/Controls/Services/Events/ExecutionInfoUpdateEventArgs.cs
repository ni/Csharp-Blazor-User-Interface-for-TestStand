using NationalInstruments.Sequencing.V2;

namespace NationalInstruments.TestStand.BlazorOI.UI.Services.Events
{
    /// <summary>
    /// Encapsulates information about execution info updates.
    /// </summary>
    internal sealed class ExecutionInfoUpdateEventArgs(int executionId, ExecutionInfoUpdate executionInfoUpdate) : EventArgs
    {
        public int ExecutionId { get; } = executionId;

        public ExecutionInfoUpdate ExecutionInfoUpdate { get; } = executionInfoUpdate;

        /// <summary>
        /// Gets the initialization info if present.
        /// </summary>
        public ExecutionInitializationInfo? InitializationInfo => ExecutionInfoUpdate.InfoCase == ExecutionInfoUpdate.InfoOneofCase.InitializationInfo
            ? ExecutionInfoUpdate.InitializationInfo
            : null;

        /// <summary>
        /// Gets the UUT info if present.
        /// </summary>
        public ExecutionUUTInfo? UUTInfo => ExecutionInfoUpdate.InfoCase == ExecutionInfoUpdate.InfoOneofCase.UutInfo
            ? ExecutionInfoUpdate.UutInfo
            : null;
    }
}