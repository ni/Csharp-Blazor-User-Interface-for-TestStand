using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.WebOI.SharedDomain.Models;
using NationalInstruments.TestStand.WebOI.UI.Services.Events;

namespace NationalInstruments.TestStand.WebOI.UI.Services
{
    /// <summary>
    /// Service for handling execution state of the active executions.
    /// </summary>
    internal interface IExecutionStateService
    {
        Execution? ActiveExecution { get; }

        IReadOnlyCollection<Execution> Executions { get; }

        void SelectActiveExecution(int execution_id);

        Task CloseExecutionAsync(int execution_id);

        Task CleanupAllExecutionsAsync();

        void UpdateExecutionState(int execution_id, ExecutionUpdate executionUpdate);

        void UpdateErrorState(int execution_id, ErrorUpdate errorUpdate);

        void UpdateExecutionStepList(int execution_id, ExecutionStepListUpdate executionStepListUpdate);

        void UpdateExecutionInfoState(int execution_id, ExecutionInfoUpdate executionInfoUpdate);

        void UpdateExecutionResult(int execution_id, ExecutionResultUpdate executionResultUpdate);

        void InvokeErrorDialog(int executionId, ErrorUpdate errorUpdate);

        event EventHandler<ObserveErrorEventArgs>? OnObserveError;

        event EventHandler<ExecutionResultUpdateEventArgs>? OnExecutionResultUpdate;

        event EventHandler<ExecutionSequenceEventArgs>? OnExecutionSequenceChange;

        event EventHandler<ExecutionStatusEventArgs>? OnExecutionStatusChange;

        event EventHandler<ExecutionTimeInfoEventArgs>? OnExecutionTimeInfoChange;

        event EventHandler? OnReportCollectionChange;

        event EventHandler<ExecutionTracingEventArgs>? OnExecutionTracingChange;

        event EventHandler? OnActiveExecutionChange;

        event EventHandler? OnExecutionListChange;

        event EventHandler<ExecutionInfoUpdateEventArgs>? OnExecutionInfoUpdate;
    }
}