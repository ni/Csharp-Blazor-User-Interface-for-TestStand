using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.BlazorOI.SharedDomain;
using NationalInstruments.TestStand.BlazorOI.SharedDomain.Models;
using NationalInstruments.TestStand.BlazorOI.UI.Services;
using NationalInstruments.TestStand.BlazorOI.UI.Services.Events;
using NimbleBlazor;

namespace NationalInstruments.TestStand.BlazorOI.UI.Components
{
    /// <summary>
    /// Represents the execution steps Pane component on the right bar of the application window.
    /// </summary>
    public sealed partial class ExecutionStepsPane : ComponentBase, IDisposable
    {
        [Inject]
        private IExecutionStateService ExecutionStateService { get; set; } = null!;

        [Inject]
        private ISequencingServiceClient SequencingServiceClient { get; set; } = null!;

        [Inject]
        private ILogger<StepsPane> Logger { get; set; } = null!;

        private Execution? _activeExecution;
        private string TableBackgroundCss { get; set; } = string.Empty;
        private NimbleTable<ExecutionStepsRecord> StepsTable { get; set; } = null!;
        private Collection<ExecutionStepsRecord> StepCollection { get; set; } = [];
        private EventHandler? _activeExecutionChangeHandler;
        private EventHandler<ExecutionTracingEventArgs>? _executionTracingHandler;
        private EventHandler<ExecutionSequenceEventArgs>? _executionSequenceHandler;
        private EventHandler<ExecutionStatusEventArgs>? _executionStatusHandler;

        private string _actionMenuBreakpointText = string.Empty;
        private ExecutionStepsRecord? _selectedStepRecord;

        /// <inheritdoc/>
        protected override async Task OnInitializedAsync()
        {
            _activeExecution = ExecutionStateService.ActiveExecution;
            UpdateTableBackgroundCssProperty();
            await UpdateStepCollectionAsync();
            _activeExecutionChangeHandler = async (s, e) => await HandleActiveExecutionChangeAsync();
            _executionSequenceHandler = async (s, e) => await HandleExecutionSequenceChangeAsync(e);
            _executionTracingHandler = async (s, e) => await HandleExecutionTracingAsync(e);
            _executionStatusHandler = async (s, e) => await HandleExecutionStatusAsync(e);
            ExecutionStateService.OnActiveExecutionChange += _activeExecutionChangeHandler;
            ExecutionStateService.OnExecutionSequenceChange += _executionSequenceHandler;
            ExecutionStateService.OnExecutionTracingChange += _executionTracingHandler;
            ExecutionStateService.OnExecutionStatusChange += _executionStatusHandler;
        }

        /// <inheritdoc/>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (StepsTable is not null && StepCollection.Count > 0)
            {
                await SetTableDataAsync(StepsTable, StepCollection);
            }
        }

        private void OnActionMenuBeforeToggle(TableActionMenuToggleEventArgs args)
        {
            string? selectedRecordId = args.RecordIds?.FirstOrDefault();
            _selectedStepRecord = StepCollection.FirstOrDefault(step => step.UniqueId == selectedRecordId);
            if (selectedRecordId is not null && _selectedStepRecord is not null)
            {
                _actionMenuBreakpointText = (_selectedStepRecord.BreakpointIcon == BreakpointIcon.Unset.ToString()) ? "Add Breakpoint" : "Remove Breakpoint";
            }
            else
            {
                _actionMenuBreakpointText = string.Empty;
            }
        }

        private async Task ToggleBreakpointAsync()
        {
            if (_selectedStepRecord is null || _activeExecution?.ExecutionId is null)
            {
                return;
            }

            if (_selectedStepRecord.BreakpointIcon != BreakpointIcon.Unset.ToString())
            {
                // Delete Breakpoint
                DeleteBreakpointRequest request = new()
                {
                    ExecutionId = _activeExecution.ExecutionId,
                    StepId = _selectedStepRecord.UniqueId,
                    SequenceName = _activeExecution.ExecutionSequence?.SequenceName,
                };
                try
                {
                    _ = await SequencingServiceClient.DeleteBreakpointAsync(request);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Assert(false, "No exception expected from DeleteBreakpointAsync RPC call.", ex.Message);
                    // Breakpoint should not be deleted in the UI if the RPC fails, hence returning here.
                    return;
                }
                _selectedStepRecord.BreakpointIcon = BreakpointIcon.Unset.ToString();

                // Update the step in the active sequence
                Step? step = FindStepInActiveSequence(_selectedStepRecord.UniqueId);
                if (step is not null)
                {
                    step.BreakpointSettings = null;
                }
            }
            else
            {
                CreateOrUpdateBreakpointRequest request = new()
                {
                    ExecutionId = _activeExecution.ExecutionId,
                    StepId = _selectedStepRecord.UniqueId,
                    SequenceName = _activeExecution.ExecutionSequence?.SequenceName,
                    BreakpointSettings = new StepBreakpointSettings
                    {
                        IsEnabled = true
                    }
                };
                try
                {
                    _ = await SequencingServiceClient.CreateOrUpdateBreakpointAsync(request);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Assert(false, "No exception expected from CreateOrUpdateBreakpointAsync RPC call.", ex.Message);
                    // Breakpoint should not be added in the UI if the RPC fails, hence returning here.
                    return;
                }
                _selectedStepRecord.BreakpointIcon = BreakpointIcon.Enabled.ToString();

                // Update the step in the active sequence
                Step? step = FindStepInActiveSequence(_selectedStepRecord.UniqueId);
                if (step is not null)
                {
                    step.BreakpointSettings = new StepBreakpointSettings
                    {
                        IsEnabled = true
                    };
                }
            }
            await InvokeAsync(StateHasChanged);
        }

        private Step? FindStepInActiveSequence(string uniqueId)
        {
            Sequence? activeSequence = ExecutionStateService.ActiveExecution?.ExecutionSequence;
            return activeSequence?.FindStepById(uniqueId);
        }

        private void UpdateTableBackgroundCssProperty()
        {
            if (_activeExecution?.ExecutionStatus is ExecutionStatus.Completed or ExecutionStatus.Aborted or ExecutionStatus.Terminated)
            {
                TableBackgroundCss = "completed-background";
            }
            else
            {
                TableBackgroundCss = _activeExecution?.ExecutionStatus is ExecutionStatus.Paused ? "paused-background" : string.Empty;
            }
        }

        private async Task UpdateStepCollectionAsync()
        {
            StepCollection.Clear();
            Sequence? executionSequence = _activeExecution?.ExecutionSequence;
            AddStepsToStepCollection(executionSequence?.SetupSteps, StepGroup.Setup, StepCollection);
            AddStepsToStepCollection(executionSequence?.MainSteps, StepGroup.Main, StepCollection);
            AddStepsToStepCollection(executionSequence?.CleanupSteps, StepGroup.Cleanup, StepCollection);
            if (StepsTable is not null)
            {
                await StepsTable.SetDataAsync(StepCollection);
            }
        }

        private void AddStepsToStepCollection(Collection<Step>? steps, StepGroup stepGroup, Collection<ExecutionStepsRecord> stepCollection)
        {
            for (int i = 0; i < steps?.Count; i++)
            {
                ExecutionStepsRecord record = new()
                {
                    UniqueId = steps[i].UniqueId,
                    Index = steps[i].Index,
                    Name = new string('\u00A0', steps[i].BlockLevel * 6) + steps[i].Name,
                    Description = steps[i].Description,
                    BlockLevel = steps[i].BlockLevel,
                    IconName = steps[i].IconName,
                    StepGroup = stepGroup.ToString(),
                    ExecutionStatus = steps[i].ExecutionInfo?.ResultStatus?.ToString(),
                    ExecutionIcon = GetResultStatusIconValue(steps[i].ExecutionInfo?.ResultStatus?.ToString()),
                    BreakpointIcon = GetBreakpointIcon(steps[i].BreakpointSettings).ToString(),
                    IsActiveStep = steps[i].ExecutionInfo?.ResultStatus?.ToString() is SequencingConstants.RunningStatus or SequencingConstants.PausedStatus
                };
                stepCollection.Add(record);
            }
        }

        private string GetResultStatusIconValue(string? resultStatus)
        {
            return resultStatus switch
            {
                SequencingConstants.PassedStatus => SequencingConstants.PassedStatus,
                SequencingConstants.FailedStatus => SequencingConstants.FailedStatus,
                SequencingConstants.ErrorStatus => SequencingConstants.ErrorStatus,
                SequencingConstants.SkippedStatus => SequencingConstants.SkippedStatus,
                SequencingConstants.DoneStatus => SequencingConstants.DoneStatus,
                SequencingConstants.RunningStatus => SequencingConstants.RunningStatus,
                SequencingConstants.AbortedStatus => SequencingConstants.AbortedStatus,
                SequencingConstants.TerminatedStatus => SequencingConstants.TerminatedStatus,
                SequencingConstants.PausedStatus => SequencingConstants.PausedStatus,
                null => string.Empty,
                _ => "Custom",
            };
        }

        private async Task SetTableDataAsync(NimbleTable<ExecutionStepsRecord> nimbleTable, Collection<ExecutionStepsRecord> dataToSet)
        {
            try
            {
                await nimbleTable.SetDataAsync(dataToSet);
            }
            catch (Exception ex)
            {
                // Most likely we will get to this block when the Nimble table already got disposed. Hence, ignoring the exception.
                Logger.LogWarning(ex, "Failed to set data on StepsTable.");
            }
        }

        private async Task HandleActiveExecutionChangeAsync()
        {
            _activeExecution = ExecutionStateService.ActiveExecution;
            await UpdateStepCollectionAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleExecutionSequenceChangeAsync(ExecutionSequenceEventArgs e)
        {
            if (e.ExecutionId == _activeExecution?.ExecutionId)
            {
                await UpdateStepCollectionAsync();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task HandleExecutionTracingAsync(ExecutionTracingEventArgs e)
        {
            if (e.ExecutionId == _activeExecution?.ExecutionId)
            {
                if (e.PreviousStep?.Step is not null && e.PreviousStep.StepIndex >= 0 && e.PreviousStep.StepIndex < StepCollection.Count)
                {
                    ExecutionStepsRecord previousStepRecord = StepCollection[e.PreviousStep.StepIndex];
                    previousStepRecord.ExecutionStatus = e.PreviousStep.Step.ExecutionInfo?.ResultStatus?.ToString();
                    previousStepRecord.ExecutionIcon = GetResultStatusIconValue(previousStepRecord.ExecutionStatus);
                    previousStepRecord.IsActiveStep = false;
                }
                if (e.NextStep is not null && e.NextStep.StepIndex >= 0 && e.NextStep.StepIndex < StepCollection.Count)
                {
                    ExecutionStepsRecord nextStepRecord = StepCollection[e.NextStep.StepIndex];
                    nextStepRecord.ExecutionStatus = SequencingConstants.RunningStatus;
                    nextStepRecord.ExecutionIcon = SequencingConstants.RunningStatus;
                    nextStepRecord.IsActiveStep = true;
                    if (StepsTable is not null)
                    {
                        await SetSelectedRecordIdsAsync(StepsTable, [nextStepRecord.UniqueId]);
                    }
                }
                if (StepsTable is not null)
                {
                    await SetTableDataAsync(StepsTable, StepCollection);
                }
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task HandleExecutionStatusAsync(ExecutionStatusEventArgs e)
        {
            if (e.ExecutionId == _activeExecution?.ExecutionId)
            {
                if (e.Status is ExecutionStatus.Aborted or ExecutionStatus.Terminated or ExecutionStatus.Paused or ExecutionStatus.Completed)
                {
                    if (e.Status is not ExecutionStatus.Paused)
                    {
                        TableBackgroundCss = "completed-background";
                        if (StepsTable is not null)
                        {
                            await SetSelectedRecordIdsAsync(StepsTable, []);
                        }
                        await InvokeAsync(StateHasChanged);
                    }
                    else
                    {
                        TableBackgroundCss = "paused-background";
                        await InvokeAsync(StateHasChanged);
                    }

                    ExecutionStepsRecord? runningStep = GetRunningStep() ?? GetPausedStep();
                    if (runningStep is not null)
                    {
                        runningStep.ExecutionStatus = e.Status.ToString();
                        runningStep.ExecutionIcon = GetResultStatusIconValue(runningStep.ExecutionStatus);
                        runningStep.IsActiveStep = e.Status == ExecutionStatus.Paused;
                        if (StepsTable is not null)
                        {
                            await SetTableDataAsync(StepsTable, StepCollection);
                        }
                        await InvokeAsync(StateHasChanged);
                    }
                }
                else if (e.Status is ExecutionStatus.Running)
                {
                    ExecutionStepsRecord? step = StepCollection.FirstOrDefault(step => string.Equals(step.ExecutionStatus, ExecutionStatus.Paused.ToString(), StringComparison.OrdinalIgnoreCase));
                    if (step is not null)
                    {
                        step.ExecutionStatus = SequencingConstants.RunningStatus;
                        step.ExecutionIcon = SequencingConstants.RunningStatus;
                        step.IsActiveStep = true;
                        TableBackgroundCss = string.Empty;
                        if (StepsTable is not null)
                        {
                            await SetTableDataAsync(StepsTable, StepCollection);
                        }
                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
        }

        private ExecutionStepsRecord? GetRunningStep()
        {
            return StepCollection.FirstOrDefault(step => string.Equals(step.ExecutionStatus, SequencingConstants.RunningStatus, StringComparison.OrdinalIgnoreCase));
        }

        private ExecutionStepsRecord? GetPausedStep()
        {
            return StepCollection.FirstOrDefault(step => string.Equals(step.ExecutionStatus, SequencingConstants.PausedStatus, StringComparison.OrdinalIgnoreCase));
        }

        private async Task SetSelectedRecordIdsAsync(NimbleTable<ExecutionStepsRecord> nimbleTable, string[] uniqueIds)
        {
            try
            {
                await nimbleTable.SetSelectedRecordIdsAsync(uniqueIds);
            }
            catch
            {
                // Most likely we will get to this block when the Nimble table already got disposed. Hence, ignoring the exception.
            }
        }

        private BreakpointIcon GetBreakpointIcon(StepBreakpointSettings breakpointSettings)
        {
            if (breakpointSettings is null)
            {
                return BreakpointIcon.Unset;
            }
            else if (!breakpointSettings.IsEnabled)
            {
                return BreakpointIcon.Disabled;
            }
            else if (!string.IsNullOrEmpty(breakpointSettings.Condition))
            {
                return BreakpointIcon.Conditional;
            }
            else
            {
                return BreakpointIcon.Enabled;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_activeExecutionChangeHandler != null)
            {
                ExecutionStateService.OnActiveExecutionChange -= _activeExecutionChangeHandler;
                _activeExecutionChangeHandler = null;
            }
            if (_executionSequenceHandler != null)
            {
                ExecutionStateService.OnExecutionSequenceChange -= _executionSequenceHandler;
                _executionSequenceHandler = null;
            }
            if (_executionStatusHandler != null)
            {
                ExecutionStateService.OnExecutionStatusChange -= _executionStatusHandler;
                _executionStatusHandler = null;
            }
            if (_executionTracingHandler != null)
            {
                ExecutionStateService.OnExecutionTracingChange -= _executionTracingHandler;
                _executionTracingHandler = null;
            }
        }
    }

    internal sealed class ExecutionStepsRecord
    {
        public string UniqueId { get; set; } = string.Empty;
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BlockLevel { get; set; }
        public string IconName { get; set; } = "unknown.ico";
        public string StepGroup { get; set; } = "Main";
        public string? ExecutionStatus { get; set; } = string.Empty;
        public string? ExecutionIcon { get; set; } = string.Empty;
        public string BreakpointIcon { get; set; } = SharedDomain.BreakpointIcon.Unset.ToString();
        public bool IsActiveStep { get; set; }
    }
}
