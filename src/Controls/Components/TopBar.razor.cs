using System.IO.Abstractions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.WebOI.SharedDomain.Models;
using NationalInstruments.TestStand.WebOI.UI.Services;
using NimbleBlazor;
using static NationalInstruments.TestStand.WebOI.UI.Services.NimbleOptions;

namespace NationalInstruments.TestStand.WebOI.UI.Components
{
    /// <summary>
    /// Represents the navigation bar component for the application.
    /// </summary>
    public sealed partial class TopBar : ComponentBase, IDisposable
    {
        [Inject]
        private IAppStateService AppStateService { get; set; } = null!;

        [Inject]
        private IUserStateService UserStateService { get; set; } = null!;

        [Inject]
        private ISequenceFileStateService SequenceFileStateService { get; set; } = null!;

        [Inject]
        private IExecutionStateService ExecutionStateService { get; set; } = null!;

        [Inject]
        private ISequencingServiceClient SequencingClient { get; set; } = null!;

        [Inject]
        private IUserManagementClient UserManagementClient { get; set; } = null!;

        [Inject]
        private ISequencingObserver SequencingObserver { get; set; } = null!;

        [Inject]
        private IFileSystem FileSystem { get; set; } = null!;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;

        [Inject]
        private ILogger<TopBar> Logger { get; set; } = null!;

        private bool IsUserNull => UserStateService.CurrentUser is null;
        private NimbleDialog<NimbleDialogResult>? _dialog;
        private string _newFilePath = string.Empty;
        private string _requiredArgumentErrorMessage = string.Empty;
        private bool _isLoading;
        private int _executionCount;

        private NimbleDialog<NimbleDialogResult>? _loginDialog;
        private string _loginDialogErrorMessage = string.Empty;
        private string _password = string.Empty;
        private string _username = "administrator";
        private bool _isLoggingOut;
        private bool _isLoggingIn = true;

        private readonly SemaphoreSlim _closeSequenceFileLock = new(1, 1);

        private EventHandler? _stateHandler;
        private EventHandler<bool>? _paneToggleHandler;
        private EventHandler? _executionListHandler;

        /// <inheritdoc/>
        protected override async Task OnInitializedAsync()
        {
            UserStateService.CurrentUser = await GetCurrentUserAsync();
            _isLoggingIn = false;
            await ObserveSequenceFileExecutionAsync();
            await StartMessageProcessingTaskAsync();
            _stateHandler = async (s, e) => await InvokeAsync(StateHasChanged);
            _paneToggleHandler = async (s, e) => await InvokeAsync(StateHasChanged);
            _executionListHandler = async (s, e) =>
            {
                _executionCount = ExecutionStateService.Executions.Count;
                await InvokeAsync(StateHasChanged);
            };
            SequenceFileStateService.OnFileOpeningClosing += _stateHandler;
            UserStateService.OnUserChange += _stateHandler;
            AppStateService.OnTogglePane += _paneToggleHandler;
            ExecutionStateService.OnExecutionListChange += _executionListHandler;
        }

        /// <inheritdoc/>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && IsUserNull)
            {
                if (!_isLoggingIn)
                {
                    await OpenLoginDialogAsync();
                }
                else
                {
                    // Wait until _isLoggingIn becomes false, then open the dialog if the user is still null.
                    while (_isLoggingIn)
                    {
                        await Task.Delay(100);
                    }
                    if (IsUserNull)
                    {
                        await OpenLoginDialogAsync();
                    }
                }
            }
        }

        private async Task StartMessageProcessingTaskAsync()
        {
            if (!SequencingObserver.IsProcessingSequenceFileMessageQueue)
            {
                await SequencingObserver.StartSequenceFileMessageProcessingAsync();
            }
            if (!SequencingObserver.IsProcessingExecutionMessageQueue)
            {
                await SequencingObserver.StartExecutionMessageProcessingAsync();
            }
        }

        internal async Task BrowseSequenceDialogAsync()
        {
            try
            {
                string? selectedFile = await JSRuntime.InvokeAsync<string>("selectFile");
                if (string.IsNullOrEmpty(selectedFile))
                {
                    return;
                }
                await OpenSequenceFileAsync(selectedFile);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error occurred while opening native file browser. Using Nimble dialog.");
                _requiredArgumentErrorMessage = string.Empty;
                _newFilePath = string.Empty;
                await InvokeAsync(StateHasChanged);
                try
                {
                    _ = await _dialog!.ShowAsync();
                }
                catch (Exception dialogEx)
                {
                    Logger.LogError(dialogEx, "Failed to show open sequence file dialog. {Message}", dialogEx.Message);
                }
            }
        }

        private async Task OpenSequenceFileAsync(string filePath)
        {
            if (_isLoading)
            {
                return;
            }

            _isLoading = true;
            try
            {
                OpenSequenceFileRequest request = new() { SequenceFileUri = filePath };
                OpenSequenceFileResponse response = await SequencingClient.OpenSequenceFileAsync(request);

                if (response != null && !string.IsNullOrEmpty(response.SequenceFileId))
                {
                    // Close previously opened sequence file if any.
                    await CloseSequenceFileAsync();
                    string fileName = string.Empty;
                    try
                    {
                        fileName = Path.GetFileName(filePath);
                    }
                    catch
                    {
                        // Ignore any exceptions from Path.GetFileName and leave it as empty string.
                        // SequenceFileName will be updated, once HeaderUpdate is received.
                    }
                    SequenceFileStateService.ActiveSequenceFile = new SequenceFile
                    {
                        SequenceFileId = response.SequenceFileId,
                        SequenceFileName = fileName,
                        Sequences = [],
                        ActiveSequence = null
                    };
                    await ObserveSequenceFileAsync(response.SequenceFileId);
                }
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task ObserveSequenceFileAsync(string sequenceFileId)
        {
            await SequencingObserver.BeginObservingSequenceFileAsync(sequenceFileId);
        }

        private async Task ObserveSequenceFileExecutionAsync()
        {
            await SequencingObserver.BeginObservingExecutionAsync();
        }

        private async Task CloseSequenceFileAsync()
        {
            await _closeSequenceFileLock.WaitAsync();
            try
            {
                if (SequenceFileStateService.IsFileOpen)
                {
                    CloseSequenceFileRequest request = new() { SequenceFileId = SequenceFileStateService.ActiveSequenceFile?.SequenceFileId };
                    _ = await SequencingClient.CloseSequenceFileAsync(request);
                    SequenceFileStateService.IsExecuting = false;
                    SequenceFileStateService.ActiveSequenceFile = null;
                }
            }
            finally
            {
                _ = _closeSequenceFileLock.Release();
            }
        }

        private bool IsValidFilePath(string seqFilePath)
        {
            return FileSystem.File.Exists(seqFilePath)
                && string.Equals(FileSystem.Path.GetExtension(seqFilePath), ".seq", StringComparison.OrdinalIgnoreCase);
        }

        private async Task CloseDialogAsync(NimbleDialogResult reason)
        {
            if (reason == NimbleDialogResult.Open)
            {
                if (!IsValidFilePath(_newFilePath))
                {
                    _requiredArgumentErrorMessage = "Valid sequence file path is required.";
                    return;
                }

                await OpenSequenceFileAsync(_newFilePath);
            }
            await _dialog!.CloseAsync(reason);
        }

        private async Task OnDialogKeyDownAsync(KeyboardEventArgs e, Func<Task> closeDialogAsync)
        {
            if (e.Code is "Enter" or "NumpadEnter")
            {
                await closeDialogAsync();
            }
        }

        private void OnInputChanged(ChangeEventArgs e)
        {
            _newFilePath = e.Value?.ToString() ?? string.Empty;
        }

        private void ToggleViewPane()
        {
            AppStateService.IsSequencePaneOpen = !AppStateService.IsSequencePaneOpen;
        }

        internal async Task OpenLoginDialogAsync()
        {
            _loginDialogErrorMessage = string.Empty;
            _username = "administrator";
            _password = string.Empty;
            try
            {
                _ = await _loginDialog!.ShowAsync();
            }
            catch (Exception ex)
            {
                // Ignoring the exception, as it happens when the dialog is already open.
                Logger.LogWarning(ex, "Failed to show login dialog. {Message}", ex.Message);
            }
        }

        private async Task CloseLoginDialogAsync(NimbleDialogResult reason)
        {
            if (reason == NimbleDialogResult.Open)
            {
                if (_isLoggingIn)
                {
                    return;
                }
                _isLoggingIn = true;
                try
                {
                    LoginRequest request = new() { Username = _username, Password = _password };
                    LoginResponse response = await UserManagementClient.LoginAsync(request);
                    if (response.User is null)
                    {
                        _loginDialogErrorMessage = "Invalid Username or Password.";
                        return;
                    }
                    UserStateService.CurrentUser = await GetCurrentUserAsync();
                }
                finally
                {
                    _isLoggingIn = false;
                }
            }
            await _loginDialog!.CloseAsync(reason);
        }

        private async Task LogoutUserAsync()
        {
            if (_isLoggingOut)
            {
                return;
            }
            _isLoggingOut = true;
            try
            {
                await CloseSequenceFileAsync();
                await ExecutionStateService.CleanupAllExecutionsAsync();
                _ = await UserManagementClient.LogoutAsync(new LogoutRequest());
                UserStateService.CurrentUser = null;
            }
            finally
            {
                _isLoggingOut = false;
            }
        }

        private async Task<User> GetCurrentUserAsync()
        {
            GetCurrentUserResponse response = await UserManagementClient.GetCurrentUserAsync(new GetCurrentUserRequest());
            return response.User;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_stateHandler != null)
            {
                SequenceFileStateService.OnFileOpeningClosing -= _stateHandler;
                UserStateService.OnUserChange -= _stateHandler;
                _stateHandler = null;
            }
            if (_paneToggleHandler != null)
            {
                AppStateService.OnTogglePane -= _paneToggleHandler;
                _paneToggleHandler = null;
            }
            if (_executionListHandler != null)
            {
                ExecutionStateService.OnExecutionListChange -= _executionListHandler;
                _executionListHandler = null;
            }
            _closeSequenceFileLock?.Dispose();
        }
    }
}
