using Microsoft.AspNetCore.Components;

namespace NationalInstruments.TestStand.WebOI.WebServer
{
    /// <summary>
    /// Main layout for the Blazor Server application, which defines where the Body containing the app content goes.
    /// </summary>
    public partial class MainLayout
    {
        [Inject]
        private NavigationManager NavigationManager { get; set; } = null!;

        private void Refresh()
        {
            NavigationManager.Refresh(forceReload: true);
        }
    }
}
