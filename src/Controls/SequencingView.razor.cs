using Microsoft.AspNetCore.Components;
using NationalInstruments.TestStand.WebOI.UI.Components;
using NationalInstruments.TestStand.WebOI.UI.Services;
using NimbleBlazor;

namespace NationalInstruments.TestStand.WebOI.UI
{
    /// <summary>
    /// Represents the sequencing view component, which manages the display and behavior of sequencing-related UI elements.
    /// </summary>
    public partial class SequencingView : ComponentBase, IDisposable
    {
        [Inject]
        private ThemeProvider ThemeProvider { get; set; } = null!;

        [Inject]
        private ISequencingServiceClientConnection SequencingServiceConnection { get; set; } = null!;

        [Inject]
        private ISequenceFileStateService SequenceFileStateService { get; set; } = null!;

        [Inject]
        private IUserStateService UserStateService { get; set; } = null!;

        [Inject]
        private IAppStateService AppStateService { get; set; } = null!;

        [Inject]
        private IExecutionStateService ExecutionStateService { get; set; } = null!;

        private TopBar? _topBarRef;
        private EventHandler? _fileOpeningClosingHandler;
        private EventHandler? _themeHandler;
        private EventHandler? _userChangeHandler;
        private EventHandler? _connectionStatusHandler;
        private EventHandler<bool>? _paneToggleHandler;
        private ConnectionStatus? _connectionStatus;
        private bool _disposed;

        /// <summary>
        /// Gets the CSS class for the current theme.
        /// </summary>
        protected string ThemeClass => ThemeProvider.CurrentTheme == Theme.Light ? "light-theme" : "dark-theme";

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            _themeHandler = async (s, e) => await InvokeAsync(StateHasChanged);
            _fileOpeningClosingHandler = async (s, e) => await HandleFileOpeningClosingAsync();
            _paneToggleHandler = async (s, e) => await InvokeAsync(StateHasChanged);
            _connectionStatusHandler = async (s, e) =>
            {
                _connectionStatus = SequencingServiceConnection.Status;
                await InvokeAsync(StateHasChanged);
            };
            _userChangeHandler = async (s, e) => await InvokeAsync(StateHasChanged);
            ThemeProvider.OnChange += _themeHandler;
            SequencingServiceConnection.OnConnectionStatusChanged += _connectionStatusHandler;
            SequenceFileStateService.OnFileOpeningClosing += _fileOpeningClosingHandler;
            UserStateService.OnUserChange += _userChangeHandler;
            AppStateService.OnTogglePane += _paneToggleHandler;
            _connectionStatus = SequencingServiceConnection.Status;
        }

        private async Task HandleFileOpeningClosingAsync()
        {
            AppStateService.IsSequencePaneOpen = true;
            await InvokeAsync(StateHasChanged);
        }

        private async Task BrowseSequenceDialogAsync()
        {
            if (_topBarRef != null)
            {
                await _topBarRef.BrowseSequenceDialogAsync();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_themeHandler != null)
                    {
                        ThemeProvider.OnChange -= _themeHandler;
                        _themeHandler = null;
                    }
                    if (_userChangeHandler != null)
                    {
                        UserStateService.OnUserChange -= _userChangeHandler;
                        _userChangeHandler = null;
                    }
                    if (_connectionStatusHandler != null)
                    {
                        SequencingServiceConnection.OnConnectionStatusChanged -= _connectionStatusHandler;
                        _connectionStatusHandler = null;
                    }
                    if (_fileOpeningClosingHandler != null)
                    {
                        SequenceFileStateService.OnFileOpeningClosing -= _fileOpeningClosingHandler;
                        _fileOpeningClosingHandler = null;
                    }
                    if (_paneToggleHandler != null)
                    {
                        AppStateService.OnTogglePane -= _paneToggleHandler;
                        _paneToggleHandler = null;
                    }
                }
                _disposed = true;
            }
        }
    }
}
