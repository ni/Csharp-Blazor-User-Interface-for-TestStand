using System.Collections.ObjectModel;
using NationalInstruments.Sequencing.V2;

namespace NationalInstruments.TestStand.WebOI.SharedDomain.Models
{
    /// <summary>
    /// The execution object
    /// </summary>
    public sealed class Execution
    {
        /// <summary>
        /// The unique Id of the execution.
        /// </summary>
        public required int ExecutionId { get; set; }

        /// <summary>
        /// The active sequence of the execution.
        /// </summary>
        public Sequence? ExecutionSequence { get; set; }

        /// <summary>
        /// Gets or sets the current execution status of the execution.
        /// </summary>
        public ExecutionStatus ExecutionStatus { get; set; } = ExecutionStatus.Unspecified;

        /// <summary>
        /// Gets or sets the execution time information for the current execution.
        /// </summary>
        public ExecutionTimeInfo ExecutionTimeInfo { get; set; } = new();

        /// <summary>
        /// Gets or sets the collection of report file locations.
        /// </summary>
        public Collection<string> ReportLocations { get; set; } = [];

        /// <summary>
        /// Gets or sets the result status of the execution.
        /// </summary>
        public string ExecutionResult { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message associated with the execution result.
        /// </summary>
        public string ExecutionResultErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the collection of errors that occurred during execution.
        /// </summary>
        public Collection<ErrorUpdate> ExecutionErrors { get; } = [];

        /// <summary>
        /// Gets or sets the entry point sequence name for the execution.
        /// </summary>
        public string? EntrySequenceName { get; set; }

        /// <summary>
        /// Gets or sets the client sequence file URI for the execution.
        /// </summary>
        public string? ClientSequenceFilePath { get; set; }

        /// <summary>
        /// Gets or sets the UUT serial number for the execution.
        /// </summary>
        public string? UUTSerialNumber { get; set; }
    }
}
