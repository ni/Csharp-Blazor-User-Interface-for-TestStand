using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.BlazorOI.SharedDomain.Models;
using NationalInstruments.TestStand.BlazorOI.UI.Services.Events;

namespace NationalInstruments.TestStand.BlazorOI.UI.Services
{
    /// <inheritdoc/>
    internal sealed class SequenceFileStateService(ILogger<SequenceFileStateService> logger) : ISequenceFileStateService
    {
        private SequenceFile? _activeSequenceFile;
        private bool _isExecuting;
        private readonly object _sequenceStateLock = new();

        public event EventHandler? OnFileOpeningClosing;
        public event EventHandler? OnSequenceFileNameUpdate;
        public event EventHandler? OnSequenceFileChange;
        public event EventHandler? OnActiveSequenceChange;
        public event EventHandler<InvokeErrorBannerEventArgs>? OnInvokeErrorBanner;
        public event EventHandler? OnSequenceFileExecutionStateChange;
        public event EventHandler? OnProcessModelEntryPointsUpdate;

        public bool IsFileOpen
        {
            get
            {
                lock (_sequenceStateLock)
                {
                    return _activeSequenceFile != null;
                }
            }
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            set
            {
                _isExecuting = value;
                OnSequenceFileExecutionStateChange?.Invoke(this, EventArgs.Empty);
            }
        }

        public SequenceFile? ActiveSequenceFile
        {
            get
            {
                lock (_sequenceStateLock)
                {
                    return _activeSequenceFile;
                }
            }
            set
            {
                lock (_sequenceStateLock)
                {
                    _activeSequenceFile = value;
                }
                logger.LogInformation("Sequence file with Id {Id} opened.", value?.SequenceFileId ?? "null");
                OnFileOpeningClosing?.Invoke(this, EventArgs.Empty);
            }
        }

        public string? ActiveSequenceFileName
        {
            get
            {
                lock (_sequenceStateLock)
                {
                    return _activeSequenceFile?.SequenceFileName;
                }
            }
        }

        public Sequence? ActiveSequence
        {
            get
            {
                lock (_sequenceStateLock)
                {
                    return _activeSequenceFile?.ActiveSequence;
                }
            }
        }

        public void UpdateActiveSequence(string sequenceName)
        {
            lock (_sequenceStateLock)
            {
                if (_activeSequenceFile == null)
                {
                    return;
                }
                Sequence? sequence = _activeSequenceFile.Sequences.FirstOrDefault(seq => string.Equals(seq.SequenceName, sequenceName, StringComparison.OrdinalIgnoreCase));
                _activeSequenceFile.ActiveSequence = sequence;
            }
            OnActiveSequenceChange?.Invoke(this, EventArgs.Empty);
        }

        public (Collection<Step> SetupSteps, Collection<Step> MainSteps, Collection<Step> CleanupSteps) ActiveSequenceStepCollection()
        {
            lock (_sequenceStateLock)
            {
                return _activeSequenceFile?.ActiveSequence is not null
                    ? (_activeSequenceFile.ActiveSequence.SetupSteps ?? [],
                        _activeSequenceFile.ActiveSequence.MainSteps ?? [],
                        _activeSequenceFile.ActiveSequence.CleanupSteps ?? [])
                    : ([], [], []);
            }
        }

        public void UpdateHeaderState(HeaderUpdate headerUpdate)
        {
            lock (_sequenceStateLock)
            {
                if (_activeSequenceFile != null
                    && !string.IsNullOrEmpty(headerUpdate.HeaderText)
                    && !string.Equals(_activeSequenceFile.SequenceFileName, headerUpdate.HeaderText, StringComparison.OrdinalIgnoreCase))
                {
                    _activeSequenceFile.SequenceFileName = headerUpdate.HeaderText;
                }
                else
                {
                    return;
                }
            }
            OnSequenceFileNameUpdate?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateSequenceFileState(SequenceFileUpdate sequenceFileUpdate)
        {
            List<Sequence> updatedSequences = [];
            foreach (AdvancedSequenceUpdate seq in sequenceFileUpdate.Sequences)
            {
                updatedSequences.Add(new()
                {
                    SequenceName = seq.SequenceName,
                    SetupSteps = [.. seq.SetupSteps],
                    MainSteps = [.. seq.MainSteps],
                    CleanupSteps = [.. seq.CleanupSteps],
                    SequenceType = seq.SequenceType
                });
            }
            lock (_sequenceStateLock)
            {
                if (_activeSequenceFile != null)
                {
                    _activeSequenceFile.Sequences = new Collection<Sequence>(updatedSequences);
                    string activeSequenceName = _activeSequenceFile.ActiveSequence?.SequenceName ?? string.Empty;
                    Sequence? sequence = _activeSequenceFile.Sequences.FirstOrDefault(seq => string.Equals(seq.SequenceName, activeSequenceName, StringComparison.OrdinalIgnoreCase));
                    _activeSequenceFile.ActiveSequence = sequence ?? _activeSequenceFile.Sequences.FirstOrDefault();
                }
            }
            OnSequenceFileChange?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateProcessModelEntryPoints(ProcessModelEntryPointsUpdate processModelEntryPointsUpdate)
        {
            lock (_sequenceStateLock)
            {
                if (_activeSequenceFile != null)
                {
                    bool entryPointsUpdated = false;
                    if (processModelEntryPointsUpdate.EntryPoints.Count != 0)
                    {
                        _activeSequenceFile.EntryPoints = [.. processModelEntryPointsUpdate.EntryPoints];
                        entryPointsUpdated = true;
                    }
                    if (!string.IsNullOrEmpty(processModelEntryPointsUpdate.ProcessModelUri))
                    {
                        _activeSequenceFile.ProcessModelName = Path.GetFileName(processModelEntryPointsUpdate.ProcessModelUri);
                        entryPointsUpdated = true;
                    }
                    if (entryPointsUpdated)
                    {
                        OnProcessModelEntryPointsUpdate?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        public void UpdateErrorState(ErrorUpdate errorUpdate)
        {
            logger.LogInformation("Error received while observing the sequence file: {Message}", errorUpdate.Message);
            InvokeErrorBanner(errorUpdate.Message);
        }

        private void InvokeErrorBanner(string message)
        {
            OnInvokeErrorBanner?.Invoke(this, new InvokeErrorBannerEventArgs(message));
        }
    }
}
