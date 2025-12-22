using Microsoft.Extensions.Logging;
using NationalInstruments.Sequencing.V2;

namespace NationalInstruments.TestStand.WebOI.UI.Services
{
    /// <inheritdoc/>
    internal sealed class UserStateService(ILogger<UserStateService> logger) : IUserStateService
    {
        private User? _currentUser;

        private readonly object _userStateLock = new();

        /// <inheritdoc/>
        public event EventHandler? OnUserChange;

        /// <inheritdoc/>
        public User? CurrentUser
        {
            get
            {
                lock (_userStateLock)
                {
                    return _currentUser;
                }
            }
            set
            {
                bool userChanged = false;
                lock (_userStateLock)
                {
                    if (_currentUser != value)
                    {
                        _currentUser = value;
                        userChanged = true;
                        logger.LogInformation("Current user changed to: {User}", string.IsNullOrEmpty(_currentUser?.FullName) ? _currentUser?.LoginName : _currentUser?.FullName);
                    }
                }
                if (userChanged)
                {
                    OnUserChange?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
