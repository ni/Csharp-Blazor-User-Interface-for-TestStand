using NimbleBlazor;

namespace NationalInstruments.TestStand.BlazorOI.UI.Services
{
    /// <summary>
    /// Provides theme management functionality for the application, allowing components to react to theme changes.
    /// </summary>
    internal sealed class ThemeProvider
    {
        private Theme _currentTheme = Theme.Dark;

        /// <summary>
        /// Event invoked when the current theme changes.
        /// </summary>
        public event EventHandler? OnChange;

        public Theme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    NotifyStateChanged();
                }
            }
        }

        private void NotifyStateChanged()
        {
            OnChange?.Invoke(this, EventArgs.Empty);
        }
    }
}
