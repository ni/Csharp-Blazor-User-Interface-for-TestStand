using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.BlazorOI.SharedDomain;
using NationalInstruments.TestStand.BlazorOI.UI.Services;
using NimbleBlazor;

namespace NationalInstruments.TestStand.BlazorOI.UI.Components
{
    /// <summary>
    /// Represents the Steps Pane component on the right bar of the application window.
    /// </summary>
    public sealed partial class StepsPane : ComponentBase, IDisposable
    {
        [Inject]
        private ISequenceFileStateService SequenceFileStateService { get; set; } = null!;

        [Inject]
        private ISequencingServiceClient SequencingServiceClient { get; set; } = null!;

        [Inject]
        private ILogger<StepsPane> Logger { get; set; } = null!;

        private NimbleTable<StepsRecord> StepsTable { get; set; } = null!;
        private Collection<StepsRecord> StepCollection { get; set; } = [];
        private EventHandler? _activeSequenceChangeHandler;
        private EventHandler? _sequenceFileChangeHandler;

        private string _actionMenuBreakpointText = string.Empty;
        private StepsRecord? _selectedStepRecord;

        /// <inheritdoc/>
        protected override async Task OnInitializedAsync()
        {
            await UpdateStepCollectionAsync();
            _activeSequenceChangeHandler = async (s, e) => await HandleActiveSequenceChangeAsync();
            _sequenceFileChangeHandler = async (s, e) => await HandleActiveSequenceChangeAsync();
            SequenceFileStateService.OnActiveSequenceChange += _activeSequenceChangeHandler;
            SequenceFileStateService.OnSequenceFileChange += _sequenceFileChangeHandler;
        }

        /// <inheritdoc/>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (StepsTable is not null && StepCollection.Count > 0)
            {
                await SetTableDataAsync(StepsTable, StepCollection);
            }
        }

        private async Task UpdateStepCollectionAsync()
        {
            StepCollection.Clear();
            (Collection<Step> setupSteps, Collection<Step> mainSteps, Collection<Step> cleanupSteps) = SequenceFileStateService.ActiveSequenceStepCollection();
            AddStepsToStepCollection(setupSteps, StepGroup.Setup, StepCollection);
            AddStepsToStepCollection(mainSteps, StepGroup.Main, StepCollection);
            AddStepsToStepCollection(cleanupSteps, StepGroup.Cleanup, StepCollection);
            if (StepsTable is not null)
            {
                await StepsTable.SetDataAsync(StepCollection);
            }
        }

        private void AddStepsToStepCollection(Collection<Step>? steps, StepGroup stepGroup, Collection<StepsRecord> stepCollection)
        {
            for (int i = 0; i < steps?.Count; i++)
            {
                StepsRecord record = new()
                {
                    UniqueId = steps[i].UniqueId,
                    Index = steps[i].Index,
                    Name = new string('\u00A0', steps[i].BlockLevel * 6) + steps[i].Name,
                    Description = steps[i].Description,
                    BlockLevel = steps[i].BlockLevel,
                    IconName = steps[i].IconName,
                    StepGroup = stepGroup.ToString(),
                    BreakpointIcon = GetBreakpointIcon(steps[i].BreakpointSettings).ToString()
                };
                stepCollection.Add(record);
            }
        }

        private BreakpointIcon GetBreakpointIcon(StepBreakpointSettings breakpointSettings)
        {
            if (breakpointSettings is null)
            {
                return BreakpointIcon.Unset;
            }
            else if (!breakpointSettings.IsEnabled)
            {
                return BreakpointIcon.Disabled;
            }
            else if (!string.IsNullOrEmpty(breakpointSettings.Condition))
            {
                return BreakpointIcon.Conditional;
            }
            else
            {
                return BreakpointIcon.Enabled;
            }
        }

        private void OnActionMenuBeforeToggle(TableActionMenuToggleEventArgs args)
        {
            string? selectedRecordId = args.RecordIds?.FirstOrDefault();
            _selectedStepRecord = StepCollection.FirstOrDefault(step => step.UniqueId == selectedRecordId);
            if (selectedRecordId is not null && _selectedStepRecord is not null)
            {
                _actionMenuBreakpointText = (_selectedStepRecord.BreakpointIcon == BreakpointIcon.Unset.ToString()) ? "Add Breakpoint" : "Remove Breakpoint";
            }
            else
            {
                _actionMenuBreakpointText = string.Empty;
            }
        }

        private async Task ToggleBreakpointAsync()
        {
            if (_selectedStepRecord is null)
            {
                return;
            }

            if (_selectedStepRecord.BreakpointIcon != BreakpointIcon.Unset.ToString())
            {
                // Delete Breakpoint
                DeleteBreakpointRequest request = new()
                {
                    SequenceFileId = SequenceFileStateService.ActiveSequenceFile!.SequenceFileId,
                    StepId = _selectedStepRecord.UniqueId,
                    SequenceName = SequenceFileStateService.ActiveSequence!.SequenceName
                };
                try
                {
                    _ = await SequencingServiceClient.DeleteBreakpointAsync(request);
                }
                catch
                {
                    // Breakpoint should not be deleted in the UI if the RPC fails, hence returning here.
                    return;
                }
                _selectedStepRecord.BreakpointIcon = BreakpointIcon.Unset.ToString();

                // Update the step in the active sequence
                Step? step = FindStepInActiveSequence(_selectedStepRecord.UniqueId);
                if (step is not null)
                {
                    step.BreakpointSettings = null;
                }
            }
            else
            {
                CreateOrUpdateBreakpointRequest request = new()
                {
                    SequenceFileId = SequenceFileStateService.ActiveSequenceFile!.SequenceFileId,
                    StepId = _selectedStepRecord.UniqueId,
                    SequenceName = SequenceFileStateService.ActiveSequence!.SequenceName,
                    BreakpointSettings = new StepBreakpointSettings
                    {
                        IsEnabled = true
                    }
                };
                try
                {
                    _ = await SequencingServiceClient.CreateOrUpdateBreakpointAsync(request);
                }
                catch
                {
                    // Breakpoint should not be added in the UI if the RPC fails, hence returning here.
                    return;
                }
                _selectedStepRecord.BreakpointIcon = BreakpointIcon.Enabled.ToString();

                // Update the step in the active sequence
                Step? step = FindStepInActiveSequence(_selectedStepRecord.UniqueId);
                if (step is not null)
                {
                    step.BreakpointSettings = new StepBreakpointSettings
                    {
                        IsEnabled = true
                    };
                }
            }
            await InvokeAsync(StateHasChanged);
        }

        private Step? FindStepInActiveSequence(string uniqueId)
        {
            SharedDomain.Models.Sequence? activeSequence = SequenceFileStateService.ActiveSequence;
            return activeSequence?.FindStepById(uniqueId);
        }

        private async Task HandleActiveSequenceChangeAsync()
        {
            await UpdateStepCollectionAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async Task SetTableDataAsync(NimbleTable<StepsRecord> nimbleTable, Collection<StepsRecord> dataToSet)
        {
            try
            {
                await nimbleTable.SetDataAsync(dataToSet);
            }
            catch (Exception ex)
            {
                // Most likely we will get to this block when the Nimble table already got disposed. Hence, ignoring the exception.
                Logger.LogWarning(ex, "Failed to set data on StepsTable.");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_activeSequenceChangeHandler != null)
            {
                SequenceFileStateService.OnActiveSequenceChange -= _activeSequenceChangeHandler;
                _activeSequenceChangeHandler = null;
            }
            if (_sequenceFileChangeHandler != null)
            {
                SequenceFileStateService.OnSequenceFileChange -= _sequenceFileChangeHandler;
                _sequenceFileChangeHandler = null;
            }
        }
    }

    internal sealed class StepsRecord
    {
        public string UniqueId { get; set; } = string.Empty;
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int BlockLevel { get; set; }
        public string IconName { get; set; } = "unknown.ico";
        public string StepGroup { get; set; } = "Main";
        public string BreakpointIcon { get; set; } = SharedDomain.BreakpointIcon.Unset.ToString();
    }
}
