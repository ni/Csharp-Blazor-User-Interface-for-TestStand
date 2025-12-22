using System.Collections.ObjectModel;

namespace NationalInstruments.TestStand.WebOI.SharedDomain.Models
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
    }
}
