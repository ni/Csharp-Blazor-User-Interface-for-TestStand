using Microsoft.AspNetCore.Components;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.BlazorOI.SharedDomain;
using NationalInstruments.TestStand.BlazorOI.SharedDomain.Models;
using NationalInstruments.TestStand.BlazorOI.UI.Services;
using NationalInstruments.TestStand.BlazorOI.UI.Services.Events;

namespace NationalInstruments.TestStand.BlazorOI.UI.Components
{
    /// <summary>
    /// Represents the Execution Pane component on the left bar of the application window.
    /// </summary>
    public sealed partial class ExecutionPane : ComponentBase, IDisposable
    {
        [Inject]
        private IExecutionStateService ExecutionStateService { get; set; } = null!;

        private List<Execution> ExecutionList { get; set; } = [];
        private EventHandler? _executionListHandler;
        private EventHandler<ExecutionStatusEventArgs>? _executionStatusHandler;
        private EventHandler<ExecutionSequenceEventArgs>? _executionSequenceChange;
        private EventHandler<ExecutionResultUpdateEventArgs>? _executionResultUpdateHandler;
        private EventHandler<ExecutionInfoUpdateEventArgs>? _executionInfoUpdateHandler;

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            ExecutionList = ExecutionStateService.Executions?.ToList() ?? [];
            _executionListHandler = async (s, e) => await UpdateExecutionListAsync();
            _executionStatusHandler = async (s, e) => await HandleExecutionStatusChangeAsync(e);
            _executionSequenceChange = async (s, e) => await InvokeAsync(StateHasChanged);
            _executionResultUpdateHandler = async (s, e) => await HandleExecutionResultUpdateAsync(e);
            _executionInfoUpdateHandler = async (s, e) => await HandleExecutionInfoUpdateAsync(e);

            ExecutionStateService.OnExecutionListChange += _executionListHandler;
            ExecutionStateService.OnExecutionStatusChange += _executionStatusHandler;
            ExecutionStateService.OnExecutionSequenceChange += _executionSequenceChange;
            ExecutionStateService.OnExecutionResultUpdate += _executionResultUpdateHandler;
            ExecutionStateService.OnExecutionInfoUpdate += _executionInfoUpdateHandler;
        }

        private string GetExecutionDisplayText(Execution execution)
        {
            List<string> parts = [];

            // Add entry sequence name if available
            if (!string.IsNullOrEmpty(execution.UUTSerialNumber))
            {
                parts.Add(string.Concat("Serial#: ", execution.UUTSerialNumber));
            }
            if (!string.IsNullOrEmpty(execution.EntrySequenceName))
            {
                parts.Add(execution.EntrySequenceName);
            }

            // Add sequence file name if available
            if (!string.IsNullOrEmpty(execution.ClientSequenceFilePath))
            {
                if (parts.Count > 0)
                {
                    parts.Add("-");
                }
                parts.Add(Path.GetFileName(execution.ClientSequenceFilePath));
            }

            // Add execution ID
            parts.Add($"[{execution.ExecutionId}]");

            // Add execution status
            parts.Add(execution.ExecutionStatus.ToString());

            if (execution.ExecutionStatus == ExecutionStatus.Completed)
            {
                if (string.IsNullOrEmpty(execution.ExecutionResultErrorMessage))
                {
                    parts.Add($"[{execution.ExecutionResult}]");
                }
                else
                {
                    parts.Add($"[{SequencingConstants.ErrorStatus} - {execution.ExecutionResultErrorMessage}]");
                }
            }

            return string.Join(" ", parts);
        }

        private string GetStatusString(string executionResult, string executionErrorMessage, ExecutionStatus status)
        {
            if (!string.IsNullOrEmpty(executionResult))
            {
                return !string.IsNullOrEmpty(executionErrorMessage) ? SequencingConstants.ErrorStatus : executionResult;
            }
            else
            {
                return status.ToString();
            }
        }

        private async Task HandleExecutionStatusChangeAsync(ExecutionStatusEventArgs e)
        {
            Execution? execution = ExecutionList.FirstOrDefault(exec => exec.ExecutionId == e.ExecutionId);
            if (execution != null)
            {
                execution.ExecutionStatus = e.Status;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task HandleExecutionResultUpdateAsync(ExecutionResultUpdateEventArgs e)
        {
            Execution? execution = ExecutionList.FirstOrDefault(exec => exec.ExecutionId == e.ExecutionId);
            if (execution != null)
            {
                execution.ExecutionResult = e.ExecutionResult;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task HandleExecutionInfoUpdateAsync(ExecutionInfoUpdateEventArgs e)
        {
            Execution? execution = ExecutionList.FirstOrDefault(exec => exec.ExecutionId == e.ExecutionId);
            if (execution != null)
            {
                if (e.InitializationInfo != null)
                {
                    execution.EntrySequenceName = e.InitializationInfo.EntrySequenceName;
                    execution.ClientSequenceFilePath = e.InitializationInfo.ClientSequenceFileUri;
                }
                else if (e.UUTInfo != null)
                {
                    execution.UUTSerialNumber = e.UUTInfo.UutSerialNumber;
                }
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task UpdateExecutionListAsync()
        {
            ExecutionList = ExecutionStateService.Executions?.ToList() ?? [];
            await InvokeAsync(StateHasChanged);
        }

        private void SelectExecution(int executionId)
        {
            ExecutionStateService.SelectActiveExecution(executionId);
            StateHasChanged();
        }

        private async Task CloseAllExecutionAsync()
        {
            await ExecutionStateService.CleanupAllExecutionsAsync();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_executionListHandler != null)
            {
                ExecutionStateService.OnExecutionListChange -= _executionListHandler;
                _executionListHandler = null;
            }
            if (_executionStatusHandler != null)
            {
                ExecutionStateService.OnExecutionStatusChange -= _executionStatusHandler;
                _executionStatusHandler = null;
            }
            if (_executionSequenceChange != null)
            {
                ExecutionStateService.OnExecutionSequenceChange -= _executionSequenceChange;
                _executionSequenceChange = null;
            }
            if (_executionResultUpdateHandler != null)
            {
                ExecutionStateService.OnExecutionResultUpdate -= _executionResultUpdateHandler;
                _executionResultUpdateHandler = null;
            }
            if (_executionInfoUpdateHandler != null)
            {
                ExecutionStateService.OnExecutionInfoUpdate -= _executionInfoUpdateHandler;
                _executionInfoUpdateHandler = null;
            }
        }
    }
}
