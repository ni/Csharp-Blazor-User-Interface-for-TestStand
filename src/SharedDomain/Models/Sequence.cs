using System.Collections.ObjectModel;
using NationalInstruments.Sequencing.V2;

namespace NationalInstruments.TestStand.BlazorOI.SharedDomain.Models
{
    /// <summary>
    /// The sequence object
    /// </summary>
    public class Sequence
    {
        /// <summary>
        /// The name of the sequence.
        /// </summary>
        public required string SequenceName { get; set; }

        /// <summary>
        /// Effective sequence type
        /// </summary>
        public SequenceType SequenceType { get; set; }

        /// <summary>
        /// The collection of steps in the Setup group of the sequence.
        /// </summary>
        public Collection<Step> SetupSteps { get; set; } = [];

        /// <summary>
        /// The collection of steps in the Main group of the sequence.
        /// </summary>
        public Collection<Step> MainSteps { get; set; } = [];

        /// <summary>
        /// The collection of steps in the Cleanup group of the sequence.
        /// </summary>
        public Collection<Step> CleanupSteps { get; set; } = [];

        /// <summary>
        /// The unique id of the active step in the sequence.
        /// </summary>
        public string? ActiveStepUniqueId { get; set; }
    }
}
