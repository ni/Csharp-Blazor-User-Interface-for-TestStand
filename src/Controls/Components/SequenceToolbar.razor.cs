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

        private bool _startingExecution;
        private EventHandler? _updateHandler;

        /// <inheritdoc/>
        protected override async Task OnInitializedAsync()
        {
            _updateHandler = async (s, e) => await InvokeAsync(StateHasChanged);
            SequenceFileStateService.OnSequenceFileNameUpdate += _updateHandler;
            SequenceFileStateService.OnSequenceFileExecutionStateChange += _updateHandler;
        }

        private async Task SinglePassExecutionAsync()
        {
            try
            {
                _startingExecution = true;
                if (SequenceFileStateService.ActiveSequenceFile is not null)
                {
                    await SequencingObserver.BeginObservingExecutionAsync();
                    string sequenceFileId = SequenceFileStateService.ActiveSequenceFile.SequenceFileId;
                    AdvancedExecuteSequenceFileRequest request = new()
                    {
                        SequenceFileId = sequenceFileId
                    };
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
                _updateHandler = null;
            }
        }
    }
}