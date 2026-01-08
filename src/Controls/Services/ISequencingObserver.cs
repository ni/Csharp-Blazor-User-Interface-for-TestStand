using System.Collections.Concurrent;
using NationalInstruments.Sequencing.V2;

namespace NationalInstruments.TestStand.BlazorOI.UI.Services
{
    /// <summary>
    /// Service for updating the AppState based on the ObserveStreamResponse.
    /// </summary>
    internal interface ISequencingObserver
    {
        BlockingCollection<ObserveSequenceFileResponse> SequenceFileMessageCollection { get; }

        BlockingCollection<ObserveSequenceFileExecutionResponse> ExecutionMessageCollection { get; }

        /// <summary>
        /// Start observing the sequence file with the given ID for changes.
        /// </summary>
        /// <param name="sequenceFileId">ID of the sequence to observe.</param>
        /// <returns>Task tracking the asynchronous work.</returns>
        Task BeginObservingSequenceFileAsync(string sequenceFileId);

        /// <summary>
        /// Start observing the sequence file execution for execution updates.
        /// </summary>
        /// <returns>Task tracking the asynchronous work.</returns>
        Task BeginObservingExecutionAsync();

        bool IsProcessingSequenceFileMessageQueue { get; }

        bool IsProcessingExecutionMessageQueue { get; }

        Task StartSequenceFileMessageProcessingAsync();

        Task StopSequenceFileMessageProcessingAsync();

        Task StartExecutionMessageProcessingAsync();

        Task StopExecutionMessageProcessingAsync();
    }
}
