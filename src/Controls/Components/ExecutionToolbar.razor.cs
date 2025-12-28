using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.WebOI.SharedDomain;
using NationalInstruments.TestStand.WebOI.SharedDomain.Models;
using NationalInstruments.TestStand.WebOI.UI.Services;
using NationalInstruments.TestStand.WebOI.UI.Services.Events;
namespace NationalInstruments.TestStand.WebOI.UI.Components
{
    /// <summary>
    /// Represents the tool bar component that has execution related controls.
    /// </summary>
    public sealed partial class ExecutionToolbar : ComponentBase, IDisposable
    {
        [Inject]
        private ISequencingServiceClient SequencingClient { get; set; } = null!;

        [Inject]
        private IExecutionStateService ExecutionStateService { get; set; } = null!;

        [Inject]
        private IAppStateService AppStateService { get; set; } = null!;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;

        [Inject]
        private ThemeProvider ThemeProvider { get; set; } = null!;

        [Inject]
        private ILogger<ExecutionToolbar> Logger { get; set; } = null!;

        private EventHandler<ExecutionStatusEventArgs>? _executionStatusChangedHandler;
        private EventHandler? _activeExecutionChangeHandler;
        private EventHandler<ExecutionResultUpdateEventArgs>? _executionResultUpdateHandler;
        private EventHandler? _onThemeChanged;

        private Execution? _activeExecution;
        private bool _isExecutionCompleted;
        private bool _canTerminateExecution;
        private bool _canBreakExecution;
        private bool _canResumeExecution;
        private bool _canTerminateAllExecution;
        private bool _canBreakAllExecution;
        private bool _canResumeAllExecution;
        private bool _canCloseExecution;
        private const int _maxReportLength = 100;      // Only truncate if report path is longer than this
        private const int _prefixLength = 45;          // Keep first 45 characters
        private const int _suffixLength = 55;          // Keep last 55 characters

        /// <inheritdoc/>
        protected override async Task OnInitializedAsync()
        {
            _activeExecution = ExecutionStateService.ActiveExecution;
            _onThemeChanged = async (s, e) => await InvokeAsync(StateHasChanged);
            _executionStatusChangedHandler = async (s, e) => await UpdateIconStatesAsync(e);
            _activeExecutionChangeHandler = async (s, e) => await HandleActiveExecutionChangeAsync();
            _executionResultUpdateHandler = async (s, e) => await HandleExecutionResultUpdateAsync(e);
            ExecutionStateService.OnExecutionStatusChange += _executionStatusChangedHandler;
            ExecutionStateService.OnActiveExecutionChange += _activeExecutionChangeHandler;
            ExecutionStateService.OnExecutionResultUpdate += _executionResultUpdateHandler;
            ThemeProvider.OnChange += _onThemeChanged;
            if (_activeExecution != null)
            {
                await UpdateIconStatesAsync(new ExecutionStatusEventArgs(
                        _activeExecution.ExecutionId,
                        ExecutionStateService.Executions.FirstOrDefault(e => e.ExecutionId == _activeExecution.ExecutionId)?.ExecutionStatus ?? ExecutionStatus.Unspecified));
            }
        }

        private string GetStatusString(string? executionResult, string? executionErrorMessage, ExecutionStatus? status)
        {
            if (!string.IsNullOrEmpty(executionResult))
            {
                return !string.IsNullOrEmpty(executionErrorMessage) ? SequencingConstants.ErrorStatus : executionResult;
            }
            else
            {
                return status?.ToString() ?? ExecutionStatus.Unspecified.ToString();
            }
        }

        private async Task HandleExecutionResultUpdateAsync(ExecutionResultUpdateEventArgs e)
        {
            if (_activeExecution != null && e.ExecutionId == _activeExecution.ExecutionId)
            {
                _activeExecution.ExecutionResult = e.ExecutionResult;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task HandleActiveExecutionChangeAsync()
        {
            _activeExecution = ExecutionStateService.ActiveExecution;
            if (_activeExecution != null)
            {
                await UpdateIconStatesAsync(new ExecutionStatusEventArgs(
                        _activeExecution.ExecutionId,
                        ExecutionStateService.Executions.FirstOrDefault(e => e.ExecutionId == _activeExecution.ExecutionId)?.ExecutionStatus ?? ExecutionStatus.Unspecified));
            }
            await InvokeAsync(StateHasChanged);
        }

        private async Task UpdateIconStatesAsync(ExecutionStatusEventArgs e)
        {
            if (_activeExecution != null && e.ExecutionId == _activeExecution.ExecutionId)
            {
                _canResumeExecution = e.Status is ExecutionStatus.Paused;
                _canBreakExecution = e.Status is ExecutionStatus.Running;
                _canTerminateExecution = _canResumeExecution || _canBreakExecution;
                _isExecutionCompleted = e.Status is ExecutionStatus.Completed or ExecutionStatus.Terminated or ExecutionStatus.Aborted;
                _canCloseExecution = e.Status is ExecutionStatus.Completed or ExecutionStatus.Aborted or ExecutionStatus.Terminated;
                _canResumeAllExecution = ExecutionStateService.Executions.Any(exec => exec.ExecutionStatus is ExecutionStatus.Paused);
                _canBreakAllExecution = ExecutionStateService.Executions.Any(exec => exec.ExecutionStatus is ExecutionStatus.Running);
                _canTerminateAllExecution = _canResumeAllExecution || _canBreakAllExecution;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task RestartExecutionAsync()
        {
            if (_activeExecution is not null)
            {
                RestartExecutionRequest request = new()
                {
                    ExecutionId = _activeExecution.ExecutionId
                };
                _ = await SequencingClient.RestartExecutionAsync(request);
            }
        }

        private async Task UpdateExecutionAsync(UpdateExecutionType updateType)
        {
            if (_activeExecution is not null)
            {
                AdvancedUpdateExecutionRequest request = new()
                {
                    ExecutionId = _activeExecution.ExecutionId,
                    UpdateType = updateType
                };
                _ = await SequencingClient.AdvancedUpdateExecutionAsync(request);
            }
        }

        private async Task UpdateAllExecutionAsync(UpdateExecutionType updateType)
        {
            AdvancedUpdateExecutionRequest request = new()
            {
                UpdateType = updateType
            };
            _ = await SequencingClient.AdvancedUpdateExecutionAsync(request);
        }

        private async Task CloseExecutionAsync()
        {
            if (_activeExecution is not null)
            {
                await ExecutionStateService.CloseExecutionAsync(_activeExecution.ExecutionId);
            }
        }

        private string GetTruncatedString(string path)
        {
            if (path.Length <= _maxReportLength)
            {
                return path;
            }

            string prefix = path[.._prefixLength];
            string suffix = path[^_suffixLength..];
            return $"{prefix}…{suffix}"; // Using single ellipsis character
        }

        private async Task OpenReportAsync(string path)
        {
            try
            {
                _ = await JSRuntime.InvokeAsync<string>("openReport", path);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to open report file: {FilePath}. {Message}", path, ex.Message);
                AppStateService.InvokeInfoBanner(
                    $"Cannot open report from external application. Please manually open from: {path}",
                    path);
            }
        }

        private async Task OpenErrorDialogAsync(ErrorUpdate errorUpdate)
        {
            if (_activeExecution is null)
            {
                return;
            }
            ExecutionStateService.InvokeErrorDialog(_activeExecution.ExecutionId, errorUpdate);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_executionStatusChangedHandler != null)
            {
                ExecutionStateService.OnExecutionStatusChange -= _executionStatusChangedHandler;
                _executionStatusChangedHandler = null;
            }
            if (_activeExecutionChangeHandler != null)
            {
                ExecutionStateService.OnActiveExecutionChange -= _activeExecutionChangeHandler;
                _activeExecutionChangeHandler = null;
            }
            if (_executionResultUpdateHandler != null)
            {
                ExecutionStateService.OnExecutionResultUpdate -= _executionResultUpdateHandler;
                _executionResultUpdateHandler = null;
            }
            if (_onThemeChanged != null)
            {
                ThemeProvider.OnChange -= _onThemeChanged;
                _onThemeChanged = null;
            }
        }
    }
}