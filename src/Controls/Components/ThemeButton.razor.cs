using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NationalInstruments.TestStand.BlazorOI.UI.Services;
using NimbleBlazor;

namespace NationalInstruments.TestStand.BlazorOI.UI.Components
{
    /// <summary>
    /// A button component that allows toggling between light and dark themes.
    /// </summary>
    public sealed partial class ThemeButton : ComponentBase, IDisposable
    {
        [Inject]
        private ThemeProvider ThemeProvider { get; set; } = null!;

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;

        private EventHandler? _handler;

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            _handler = async (s, e) => await InvokeAsync(StateHasChanged);
            ThemeProvider.OnChange += _handler;
        }

        /// <summary>
        /// Toggles the current theme between light and dark modes.
        /// </summary>
        private async Task ToggleThemeAsync()
        {
            ThemeProvider.CurrentTheme = ThemeProvider.CurrentTheme == Theme.Light
                ? Theme.Dark
                : Theme.Light;
            await JSRuntime.InvokeVoidAsync("toggleTheme");
        }

        /// <summary>
        /// Releases resources used by the component.
        /// </summary>
        public void Dispose()
        {
            if (_handler != null)
            {
                ThemeProvider.OnChange -= _handler;
                _handler = null;
            }
        }
    }
}