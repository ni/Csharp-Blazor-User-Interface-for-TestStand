using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.WebOI.SharedDomain.Models;
using NationalInstruments.TestStand.WebOI.UI.Services;

namespace NationalInstruments.TestStand.WebOI.UI.Components
{
    /// <summary>
    /// Represents the Sequence Pane component on the left bar of the application window.
    /// </summary>
    public sealed partial class SequencePane : ComponentBase, IDisposable
    {
        [Inject]
        private ISequenceFileStateService SequenceFileStateService { get; set; } = null!;

        private Collection<Sequence> SequenceList { get; set; } = [];
        private EventHandler? _sequenceListHandler;

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            SequenceList = SequenceFileStateService.ActiveSequenceFile?.Sequences ?? [];
            _sequenceListHandler = async (s, e) => await UpdateSequencesListAsync();
            SequenceFileStateService.OnSequenceFileChange += _sequenceListHandler;
        }

        private async Task UpdateSequencesListAsync()
        {
            SequenceList = SequenceFileStateService.ActiveSequenceFile?.Sequences ?? [];
            await InvokeAsync(StateHasChanged);
        }

        private void SelectSequence(string sequenceName)
        {
            SequenceFileStateService.UpdateActiveSequence(sequenceName);
            StateHasChanged();
        }

        // Icons used here are placeholders and will be replaces once final icons are added to the nimble library
        //  Unknown/unmapped SequenceType values intentionally fall back to the default icon to avoid breaking UI rendering.
        private string GetSequenceIconByType(SequenceType sequenceType)
        {
            string iconFile = sequenceType switch
            {
                SequenceType.Callback => "SeqCallback.ico",
                SequenceType.ExeEntryPoint => "SeqExeEntryPoint.ico",
                SequenceType.CfgEntryPoint => "SeqCfgEntryPoint.ico",
                SequenceType.ReservedCallback => "SeqReservedCallback.ico",
                SequenceType.Unspecified or
                SequenceType.Normal or
                _ => "SeqNormal.ico"
            };
            return Path.Combine("/icons/SequencesIcons", iconFile);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_sequenceListHandler != null)
            {
                SequenceFileStateService.OnSequenceFileChange -= _sequenceListHandler;
                _sequenceListHandler = null;
            }
        }
    }
}
