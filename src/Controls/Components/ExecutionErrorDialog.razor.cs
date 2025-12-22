using Microsoft.AspNetCore.Components;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.WebOI.UI.Services;
using NationalInstruments.TestStand.WebOI.UI.Services.Events;
using NimbleBlazor;
using static NationalInstruments.TestStand.WebOI.UI.Services.NimbleOptions;
namespace NationalInstruments.TestStand.WebOI.UI.Components
{
    /// <summary>
    /// Represents a dialog component that displays error information when an error is observed in the application.
    /// </summary>
    public sealed partial class ExecutionErrorDialog : ComponentBase, IDisposable
    {
        [Inject]
        private IExecutionStateService ExecutionStateService { get; set; } = null!;
        private string Message { get; set; } = string.Empty;
        private int ErrorCode { get; set; }
        private Step? ErrorStep { get; set; }
        private EventHandler<ObserveErrorEventArgs>? _observeErrorHandler;
        private NimbleDialog<NimbleDialogResult> _errorDialog = null!;
        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            _observeErrorHandler = async (s, e) => await HandleObserveErrorAsync(e);
            ExecutionStateService.OnObserveError += _observeErrorHandler;
        }
        private async Task HandleObserveErrorAsync(ObserveErrorEventArgs e)
        {
            ErrorCode = e.ErrorCode;
            Message = e.Message ?? "Error while observing the execution...";
            if (e.Step != null)
            {
                ErrorStep = e.Step;
            }
            // Close any existing dialog before showing a new one
            await CloseDialogAsync(NimbleDialogResult.Cancel);
            await InvokeAsync(StateHasChanged);
            _ = await _errorDialog.ShowAsync();
        }
        private async Task CloseDialogAsync(NimbleDialogResult nimbleDialogResult)
        {
            try
            {
                await _errorDialog.CloseAsync(nimbleDialogResult);
            }
            catch
            {
                // The dialog has been disposed, no action needed
                return;
            }
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            if (_observeErrorHandler != null)
            {
                ExecutionStateService.OnObserveError -= _observeErrorHandler;
                _observeErrorHandler = null;
            }
        }
    }
}