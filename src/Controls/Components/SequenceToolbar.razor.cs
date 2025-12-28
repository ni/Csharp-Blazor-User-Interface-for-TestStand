using Microsoft.AspNetCore.Components;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.WebOI.UI.Services;
namespace NationalInstruments.TestStand.WebOI.UI.Components
{
    /// <summary>
    /// Represents the tool bar component in the sequence view tab.
    /// </summary>
    public sealed partial class SequenceToolbar : ComponentBase, IDisposable
    {
        [Inject]
        private ISequencingServiceClient SequencingClient { get; set; } = null!;

        [Inject]
        private ISequenceFileStateService SequenceFileStateService { get; set; } = null!;

        [Inject]
        private ISequencingObserver SequencingObserver { get; set; } = null!;

        [Inject]
        private ThemeProvider ThemeProvider { get; set; } = null!;

        private List<EntryPointInfo> _entryPoints = [];
        private string _modelSequenceName = string.Empty;
        private bool _startingExecution;
        private EventHandler? _updateHandler;
        private EventHandler? _entryPointsUpdateHandler;

        /// <inheritdoc/>
        protected override async Task OnInitializedAsync()
        {
            _entryPoints = SequenceFileStateService.ActiveSequenceFile?.EntryPoints.ToList() ?? [];
            _modelSequenceName = GetFormattedProcessModelName(SequenceFileStateService.ActiveSequenceFile?.ProcessModelName);
            _updateHandler = async (s, e) => await InvokeAsync(StateHasChanged);
            _entryPointsUpdateHandler = async (s, e) => await UpdateEntryPointsAsync();
            SequenceFileStateService.OnSequenceFileNameUpdate += _updateHandler;
            SequenceFileStateService.OnSequenceFileExecutionStateChange += _updateHandler;
            SequenceFileStateService.OnProcessModelEntryPointsUpdate += _entryPointsUpdateHandler;
            SequenceFileStateService.OnActiveSequenceChange += _updateHandler;
            ThemeProvider.OnChange += _updateHandler;
        }

        private async Task UpdateEntryPointsAsync()
        {
            if (SequenceFileStateService.ActiveSequenceFile is not null)
            {
                _entryPoints = [.. SequenceFileStateService.ActiveSequenceFile.EntryPoints];
                _modelSequenceName = GetFormattedProcessModelName(SequenceFileStateService.ActiveSequenceFile?.ProcessModelName);
                await InvokeAsync(StateHasChanged);
            }
        }

        private string GetFormattedProcessModelName(string? processModelName)
        {
            return string.IsNullOrEmpty(processModelName) ? string.Empty : string.Concat(["(", processModelName, ") "]);
        }

        private async Task StartExecutionAsync(string entryPointSequenceName, bool isProcessModelEntryPoint)
        {
            try
            {
                if (SequenceFileStateService.IsExecuting)
                {
                    return;
                }
                _startingExecution = true;
                if (SequenceFileStateService.ActiveSequenceFile is not null)
                {
                    await SequencingObserver.BeginObservingExecutionAsync();
                    string sequenceFileId = SequenceFileStateService.ActiveSequenceFile.SequenceFileId;
                    AdvancedExecuteSequenceFileRequest request = new()
                    {
                        SequenceFileId = sequenceFileId
                    };
                    if (isProcessModelEntryPoint)
                    {
                        request.ProcessModelEntryName = entryPointSequenceName;
                    }
                    else
                    {
                        request.SequenceName = entryPointSequenceName;
                    }
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            SequenceFileStateService.IsExecuting = true;
                            _ = await SequencingClient.AdvancedExecuteSequenceFileAsync(request);
                        }
                        finally
                        {
                            if (SequenceFileStateService.ActiveSequenceFile?.SequenceFileId == sequenceFileId)
                            {
                                SequenceFileStateService.IsExecuting = false;
                            }
                        }
                    });
                }
            }
            finally
            {
                _startingExecution = false;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_updateHandler != null)
            {
                SequenceFileStateService.OnSequenceFileNameUpdate -= _updateHandler;
                SequenceFileStateService.OnSequenceFileExecutionStateChange -= _updateHandler;
                SequenceFileStateService.OnActiveSequenceChange -= _updateHandler;
                ThemeProvider.OnChange -= _updateHandler;
                _updateHandler = null;
            }
            if (_entryPointsUpdateHandler != null)
            {
                SequenceFileStateService.OnProcessModelEntryPointsUpdate -= _entryPointsUpdateHandler;
                _entryPointsUpdateHandler = null;
            }
        }
    }
}