using System.Collections.ObjectModel;
using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.WebOI.SharedDomain.Models;
using NationalInstruments.TestStand.WebOI.UI.Services.Events;

namespace NationalInstruments.TestStand.WebOI.UI.Services
{
    /// <summary>
    /// Service for handling the sequence file state of the application.
    /// </summary>
    internal interface ISequenceFileStateService
    {
        bool IsFileOpen { get; }

        bool IsExecuting { get; set; }

        SequenceFile? ActiveSequenceFile { get; set; }

        string? ActiveSequenceFileName { get; }

        Sequence? ActiveSequence { get; }

        void UpdateActiveSequence(string sequenceName);

        (Collection<Step> SetupSteps, Collection<Step> MainSteps, Collection<Step> CleanupSteps) ActiveSequenceStepCollection();

        void UpdateSequenceFileState(SequenceFileUpdate sequenceFileUpdate);

        void UpdateHeaderState(HeaderUpdate headerUpdate);

        void UpdateErrorState(ErrorUpdate errorUpdate);

        event EventHandler? OnFileOpeningClosing;

        event EventHandler? OnSequenceFileNameUpdate;

        event EventHandler? OnActiveSequenceChange;

        event EventHandler? OnSequenceFileChange;

        event EventHandler? OnSequenceFileExecutionStateChange;

        event EventHandler<InvokeErrorBannerEventArgs>? OnInvokeErrorBanner;
    }
}
