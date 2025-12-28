using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.WebOI.SharedDomain.Models;
using NationalInstruments.TestStand.WebOI.UI.Services.Events;

namespace NationalInstruments.TestStand.WebOI.UI.Services
{
    internal sealed class ExecutionStateService(IAppStateService appStateService, ILogger<ExecutionStateService> logger, ISequencingServiceClient sequencingServiceClient) : IExecutionStateService
    {
        private readonly ConcurrentDictionary<int, Execution> _executions = [];
        private int _activeExecutionId = -1;
        private readonly object _executionStateLock = new();

        public event EventHandler<ObserveErrorEventArgs>? OnObserveError;
        public event EventHandler<ExecutionResultUpdateEventArgs>? OnExecutionResultUpdate;
        public event EventHandler<ExecutionSequenceEventArgs>? OnExecutionSequenceChange;
        public event EventHandler<ExecutionStatusEventArgs>? OnExecutionStatusChange;
        public event EventHandler<ExecutionTimeInfoEventArgs>? OnExecutionTimeInfoChange;
        public event EventHandler<ExecutionTracingEventArgs>? OnExecutionTracingChange;
        public event EventHandler? OnReportCollectionChange;
        public event EventHandler? OnActiveExecutionChange;
        public event EventHandler? OnExecutionListChange;

        public Execution? ActiveExecution
        {
            get
            {
                lock (_executionStateLock)
                {
                    return _executions.TryGetValue(_activeExecutionId, out Execution? activeExecution) ? activeExecution : null;
                }
            }
        }

        public IReadOnlyCollection<Execution> Executions => [.. _executions.Values];

        public void SelectActiveExecution(int executionId)
        {
            lock (_executionStateLock)
            {
                if (_executions.ContainsKey(executionId))
                {
                    _activeExecutionId = executionId;
                }
                else
                {
                    return;
                }
            }
            OnActiveExecutionChange?.Invoke(this, EventArgs.Empty);
        }

        public async Task CloseExecutionAsync(int executionId)
        {
            bool activeExecutionClosed = false;
            CloseExecutionRequest request = new()
            {
                ExecutionId = executionId
            };
            _ = await sequencingServiceClient.CloseExecutionAsync(request);
            lock (_executionStateLock)
            {
                _ = _executions.TryRemove(executionId, out _);
                if (_activeExecutionId == executionId)
                {
                    _activeExecutionId = !_executions.IsEmpty ? _executions.First().Key : -1;
                    activeExecutionClosed = true;
                }
            }
            OnExecutionListChange?.Invoke(this, EventArgs.Empty);
            if (activeExecutionClosed)
            {
                OnActiveExecutionChange?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task CleanupAllExecutionsAsync()
        {
            _executions.Keys.ToList().ForEach(async executionId =>
            {
                if (_executions.TryGetValue(executionId, out Execution? execution))
                {
                    if (execution.ExecutionStatus is ExecutionStatus.Completed or ExecutionStatus.Aborted or ExecutionStatus.Terminated)
                    {
                        await CloseExecutionAsync(executionId);
                    }
                }
            });
        }

        public void UpdateExecutionStepList(int executionId, ExecutionStepListUpdate executionStepListUpdate)
        {
            Sequence sequence = new()
            {
                SequenceName = executionStepListUpdate.Sequence.SequenceName,
                SetupSteps = [.. executionStepListUpdate.Sequence.SetupSteps],
                MainSteps = [.. executionStepListUpdate.Sequence.MainSteps],
                CleanupSteps = [.. executionStepListUpdate.Sequence.CleanupSteps]
            };
            bool activeExecutionUpdated = false;
            bool executionListChanged = false;
            bool activeExecutionChanged = false;
            lock (_executionStateLock)
            {
                if (_executions.TryGetValue(executionId, out Execution? execution))
                {
                    execution.ExecutionSequence = sequence;
                }
                else
                {
                    _executions[executionId] = new Execution
                    {
                        ExecutionId = executionId,
                        ExecutionStatus = ExecutionStatus.Initializing,
                        ExecutionSequence = sequence
                    };
                    executionListChanged = true;
                    _activeExecutionId = executionId;
                    activeExecutionChanged = true;
                }
                if (_activeExecutionId == executionId)
                {
                    activeExecutionUpdated = true;
                }
            }
            if (activeExecutionUpdated)
            {
                OnExecutionSequenceChange?.Invoke(this, new ExecutionSequenceEventArgs(executionId, sequence));
            }
            if (executionListChanged)
            {
                OnExecutionListChange?.Invoke(this, EventArgs.Empty);
            }
            if (activeExecutionChanged)
            {
                OnActiveExecutionChange?.Invoke(this, EventArgs.Empty);
                appStateService.IsSequencePaneOpen = false;
            }
        }

        public void UpdateExecutionResult(int executionId, ExecutionResultUpdate executionResultUpdate)
        {
            lock (_executionStateLock)
            {
                if (_executions.TryGetValue(executionId, out Execution? execution))
                {
                    execution.ExecutionResult = executionResultUpdate.Status;
                    if (!string.IsNullOrEmpty(executionResultUpdate.ErrorMessage))
                    {
                        execution.ExecutionResultErrorMessage = executionResultUpdate.ErrorMessage;

                        if (!execution.ExecutionErrors.Any(e => e.ErrorCode == executionResultUpdate.ErrorCode && executionResultUpdate.ErrorMessage.Contains(e.Message)))
                        {
                            execution.ExecutionErrors.Add(new ErrorUpdate() { ErrorCode = executionResultUpdate.ErrorCode, Message = executionResultUpdate.ErrorMessage });
                        }
                    }
                }
            }
            OnExecutionResultUpdate?.Invoke(this, new ExecutionResultUpdateEventArgs(executionId, executionResultUpdate.Status));
        }

        public void UpdateErrorState(int executionId, ErrorUpdate errorUpdate)
        {
            lock (_executionStateLock)
            {
                if (_executions.TryGetValue(executionId, out Execution? execution))
                {
                    execution.ExecutionErrors.Add(errorUpdate);
                    _activeExecutionId = executionId;
                    if (appStateService.IsSequencePaneOpen)
                    {
                        appStateService.IsSequencePaneOpen = false;
                    }
                }
            }
            OnActiveExecutionChange?.Invoke(this, EventArgs.Empty);
            InvokeErrorDialog(executionId, errorUpdate);
        }

        public void UpdateExecutionState(int executionId, ExecutionUpdate executionUpdate)
        {
            bool executionListChanged = false;
            bool activeExecutionChanged = false;
            bool executionStatusChanged = false;
            bool activeExecutionTimeInfoChanged = false;
            bool activeReportCollectionChanged = false;
            bool activeExecutionTracingChanged = false;
            StepLocation? previousStep = null;
            StepLocation? nextStep = null;
            lock (_executionStateLock)
            {
                if (executionUpdate.ExecutionStatus is ExecutionStatus.Initializing)
                {
                    if (_executions.TryGetValue(executionId, out Execution? exec))
                    {
                        exec.ExecutionStatus = ExecutionStatus.Initializing;
                        exec.ExecutionResult = string.Empty;
                        exec.ExecutionErrors.Clear();
                        exec.ReportLocations.Clear();
                        executionStatusChanged = true;
                    }
                    else
                    {
                        _executions[executionId] = new Execution
                        {
                            ExecutionId = executionId,
                            ExecutionStatus = ExecutionStatus.Initializing,
                        };
                        executionListChanged = true;
                        _activeExecutionId = executionId;
                        activeExecutionChanged = true;
                    }
                }
                else if (_executions.TryGetValue(executionId, out Execution? execution))
                {
                    bool isActiveExecution = _activeExecutionId == executionId;
                    Sequence? executionSequence = execution.ExecutionSequence;
                    if (executionUpdate.ExecutionStatus is not ExecutionStatus.Unspecified && executionUpdate.ExecutionStatus != execution.ExecutionStatus)
                    {
                        execution.ExecutionStatus = executionUpdate.ExecutionStatus;
                        if (executionSequence is not null)
                        {
                            List<Step> allSteps = [.. executionSequence.GetAllSteps()];
                            if (executionUpdate.ExecutionStatus is ExecutionStatus.Aborted or ExecutionStatus.Terminated or ExecutionStatus.Paused or ExecutionStatus.Completed)
                            {
                                Step? currentStep = allSteps.FirstOrDefault(step => string.Equals(step.ExecutionInfo?.ResultStatus, ExecutionStatus.Running.ToString(), StringComparison.OrdinalIgnoreCase))
                                                        ?? allSteps.FirstOrDefault(step => string.Equals(step.ExecutionInfo?.ResultStatus, ExecutionStatus.Paused.ToString(), StringComparison.OrdinalIgnoreCase));
                                if (currentStep is not null)
                                {
                                    currentStep.ExecutionInfo.ResultStatus = executionUpdate.ExecutionStatus.ToString();
                                }
                            }
                            else if (executionUpdate.ExecutionStatus is ExecutionStatus.Running)
                            {
                                Step? pausedStep = allSteps.FirstOrDefault(step => string.Equals(step.ExecutionInfo?.ResultStatus, ExecutionStatus.Paused.ToString(), StringComparison.OrdinalIgnoreCase));
                                if (pausedStep is not null)
                                {
                                    pausedStep.ExecutionInfo.ResultStatus = executionUpdate.ExecutionStatus.ToString();
                                }
                            }
                        }
                        executionStatusChanged = true;
                    }
                    if (executionUpdate.ExecutionTimeInfo != null)
                    {
                        execution.ExecutionTimeInfo = executionUpdate.ExecutionTimeInfo;
                        activeExecutionTimeInfoChanged = isActiveExecution || activeExecutionTimeInfoChanged;
                    }
                    if (executionUpdate.ReportLocations.Count > 0 || executionUpdate.ExecutionStatus is ExecutionStatus.Completed)
                    {
                        execution.ReportLocations = [.. executionUpdate.ReportLocations];
                        activeReportCollectionChanged = isActiveExecution || activeReportCollectionChanged;
                    }

                    if (executionSequence != null)
                    {
                        List<Step> allSteps = [.. executionSequence.GetAllSteps()];

                        if (executionUpdate.PreviousStep != null)
                        {
                            Step? pStep = allSteps.FirstOrDefault(step => step.UniqueId == executionUpdate.PreviousStep.UniqueId);
                            if (pStep is not null)
                            {
                                int index = allSteps.IndexOf(pStep);
                                if (index >= 0)
                                {
                                    previousStep = new StepLocation(executionUpdate.PreviousStep, index);
                                    pStep.ExecutionInfo = executionUpdate.PreviousStep.ExecutionInfo;
                                    activeExecutionTracingChanged = true;
                                }
                            }
                        }
                        if (executionUpdate.NextStep != null)
                        {
                            Step? nStep = allSteps.FirstOrDefault(step => step.UniqueId == executionUpdate.NextStep.UniqueId);
                            if (nStep is not null)
                            {
                                int index = allSteps.IndexOf(nStep);
                                if (index >= 0)
                                {
                                    nStep.ExecutionInfo = executionUpdate.NextStep.ExecutionInfo;
                                    executionSequence.ActiveStepUniqueId = nStep.UniqueId;
                                    if (string.IsNullOrEmpty(nStep.ExecutionInfo.ResultStatus))
                                    {
                                        nStep.ExecutionInfo.ResultStatus = ExecutionStatus.Running.ToString();
                                        executionUpdate.NextStep.ExecutionInfo.ResultStatus = ExecutionStatus.Running.ToString();
                                    }
                                    nextStep = new StepLocation(executionUpdate.NextStep, index);
                                    activeExecutionTracingChanged = true;
                                }
                            }
                        }
                    }
                }
            }
            if (executionListChanged)
            {
                OnExecutionListChange?.Invoke(this, EventArgs.Empty);
            }
            if (activeExecutionChanged)
            {
                OnActiveExecutionChange?.Invoke(this, EventArgs.Empty);
                appStateService.IsSequencePaneOpen = false;
            }
            if (executionStatusChanged)
            {
                OnExecutionStatusChange?.Invoke(this, new ExecutionStatusEventArgs(executionId, executionUpdate.ExecutionStatus));
            }
            if (activeExecutionTimeInfoChanged)
            {
                OnExecutionTimeInfoChange?.Invoke(this, new ExecutionTimeInfoEventArgs(executionId, executionUpdate.ExecutionTimeInfo!));
            }
            if (activeReportCollectionChanged)
            {
                OnReportCollectionChange?.Invoke(this, EventArgs.Empty);
            }
            if (activeExecutionTracingChanged)
            {
                OnExecutionTracingChange?.Invoke(this, new ExecutionTracingEventArgs(executionId, previousStep, nextStep));
            }
        }

        public void InvokeErrorDialog(int executionId, ErrorUpdate errorUpdate)
        {
            NotifyObservedError(executionId, errorUpdate);
        }

        private void NotifyObservedError(int executionId, ErrorUpdate errorUpdate)
        {
            logger.LogError(
                "Error reported. RPC name: {RpcName}, Original message: {Message}, Call stack: {CallStack}, Error code: {ErrorCode}",
                errorUpdate.RpcName,
                errorUpdate.Message,
                errorUpdate.CallStack,
                errorUpdate.ErrorCode);
            OnObserveError?.Invoke(this, new ObserveErrorEventArgs(executionId, errorUpdate));
        }
    }
}
