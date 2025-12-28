using NationalInstruments.Sequencing.V2;

namespace NationalInstruments.TestStand.WebOI.UI.Services.Events
{
    /// <summary>
    /// Encapsulates information about an error involving an observed sequence.
    /// </summary>
    /// <param name="executionId">The unique identifier for the execution.</param>
    /// <param name="errorUpdate">Information from the service about the error.</param>
    internal sealed class ObserveErrorEventArgs(int executionId, ErrorUpdate errorUpdate) : EventArgs
    {
        public int ExecutionId { get; } = executionId;

        /// <inheritdoc cref="ErrorUpdate.Message" />
        public string? Message { get; } = errorUpdate.Message;

        /// <inheritdoc cref="ErrorUpdate.SequenceName" />
        public string? SequenceName { get; } = errorUpdate.SequenceName;

        /// <inheritdoc cref="ErrorUpdate.SequenceFileUri" />
        public string? SequenceFileUri { get; } = errorUpdate.SequenceFileUri;

        /// <inheritdoc cref="ErrorUpdate.ErrorCode" />
        public int ErrorCode { get; } = errorUpdate.ErrorCode;

        /// <inheritdoc cref="ErrorUpdate.Step" />
        public Step? Step { get; } = errorUpdate.Step;
    }
}