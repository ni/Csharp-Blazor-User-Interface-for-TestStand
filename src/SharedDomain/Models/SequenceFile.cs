using System.Collections.ObjectModel;
using NationalInstruments.Sequencing.V2;

namespace NationalInstruments.TestStand.BlazorOI.SharedDomain.Models
{
    /// <summary>
    /// The sequence file object
    /// </summary>
    public class SequenceFile
    {
        /// <summary>
        /// The unique Id of the sequence file.
        /// </summary>
        public required string SequenceFileId { get; set; }

        /// <summary>
        /// The name of the sequence file.
        /// </summary>
        public string SequenceFileName { get; set; } = string.Empty;

        /// <summary>
        /// The collection of sequences in the sequence file.
        /// </summary>
        public Collection<Sequence> Sequences { get; set; } = [];

        /// <summary>
        /// The active sequence object.
        /// </summary>
        public Sequence? ActiveSequence { get; set; }

        /// <summary>
        /// The applicable entry points for this sequence file, based on its associated process model.
        /// </summary>
        public Collection<EntryPointInfo> EntryPoints { get; set; } = [];

        /// <summary>
        /// Represents the name of the process model associated with this sequence file.
        /// </summary>
        public string ProcessModelName { get; set; } = string.Empty;
    }
}
