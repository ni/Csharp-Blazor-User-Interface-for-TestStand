using NationalInstruments.Sequencing.V2;

namespace NationalInstruments.TestStand.BlazorOI.UI.Services
{
    /// <summary>
    /// Service for handling the current state of the user.
    /// </summary>
    internal interface IUserStateService
    {
        User? CurrentUser { get; set; }

        event EventHandler? OnUserChange;
    }
}
