using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
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

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;

        [Inject]
        private ILogger<ExecutionErrorDialog> Logger { get; set; } = null!;

        private string Message { get; set; } = string.Empty;
        private string SequenceName { get; set; } = string.Empty;
        private string SequenceFileName { get; set; } = string.Empty;
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
            SequenceName = e.SequenceName ?? string.Empty;
            SequenceFileName = Path.GetFileName(e.SequenceFileUri ?? string.Empty);
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

        private async Task OnDialogKeyDownAsync(KeyboardEventArgs e)
        {
            if (e.CtrlKey && string.Equals(e.Key, "C", StringComparison.OrdinalIgnoreCase))
            {
                await CopyErrorDetailsAsync();
            }
        }

        private async Task CopyErrorDetailsAsync()
        {
            if (ErrorStep != null)
            {
                string errorDetails = $"{Message}\n\n\n" +
                                      $"ERROR CODE \n " +
                                      $"{ErrorCode}\n\n\n" +
                                      $"LOCATION \n " +
                                      $"Step '{ErrorStep.Name}' of sequence '{SequenceName}' in '{SequenceFileName}'";
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", errorDetails);
            }
        }

        private async Task OpenCodeHelpAsync()
        {
            string codeHelpUrl = "https://www.ni.com/docs/en-US/bundle/teststand-api-reference/page/tsapiref/tserror.html";
            try
            {
                await JSRuntime.InvokeVoidAsync("openUrlInBrowser", codeHelpUrl);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Failed to open Url using Electron API. Falling back to window.open. {Message}", ex.Message);
                await JSRuntime.InvokeVoidAsync("window.open", codeHelpUrl, "_blank");
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