using Microsoft.AspNetCore.Components;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.WebOI.SharedDomain;
using NationalInstruments.TestStand.WebOI.SharedDomain.Models;
using NationalInstruments.TestStand.WebOI.UI.Services;
using NationalInstruments.TestStand.WebOI.UI.Services.Events;

namespace NationalInstruments.TestStand.WebOI.UI.Components
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

        private const int _maxLength = 15;
        private const int _prefixLength = 10;
        private const int _suffixLength = 5;

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            ExecutionList = ExecutionStateService.Executions?.ToList() ?? [];
            _executionListHandler = async (s, e) => await UpdateExecutionListAsync();
            _executionStatusHandler = async (s, e) => await HandleExecutionStatusChangeAsync(e);
            _executionSequenceChange = async (s, e) => await InvokeAsync(StateHasChanged);
            _executionResultUpdateHandler = async (s, e) => await HandleExecutionResultUpdateAsync(e);
            ExecutionStateService.OnExecutionListChange += _executionListHandler;
            ExecutionStateService.OnExecutionStatusChange += _executionStatusHandler;
            ExecutionStateService.OnExecutionSequenceChange += _executionSequenceChange;
            ExecutionStateService.OnExecutionResultUpdate += _executionResultUpdateHandler;
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

        private string GetTruncatedExecutionSequenceName(string? sequenceName)
        {
            if (string.IsNullOrEmpty(sequenceName))
            {
                return string.Empty;
            }

            if (sequenceName.Length <= _maxLength)
            {
                return sequenceName;
            }

            string prefix = sequenceName[.._prefixLength];
            string suffix = sequenceName[^_suffixLength..];
            return $"{prefix}…{suffix}";
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
        }
    }
}
