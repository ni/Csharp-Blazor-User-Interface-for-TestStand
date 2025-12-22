using NationalInstruments.Sequencing.V2;
using NationalInstruments.TestStand.WebOI.SharedDomain.Models;

namespace NationalInstruments.TestStand.WebOI.UI.Services
{
    /// <summary>
    /// Extension methods and utilities for working with sequences and steps.
    /// </summary>
    internal static class SequenceExtensions
    {
        /// <summary>
        /// Gets all steps from the sequence across all groups (Setup, Main, Cleanup).
        /// </summary>
        /// <param name="sequence">The sequence to retrieve steps from.</param>
        public static IEnumerable<Step> GetAllSteps(this Sequence sequence)
        {
            if (sequence is null)
            {
                return [];
            }
            return (sequence.SetupSteps ?? [])
                .Concat(sequence.MainSteps ?? [])
                .Concat(sequence.CleanupSteps ?? []);
        }

        /// <summary>
        /// Finds a step by unique ID in the sequence.
        /// </summary>
        /// <param name="sequence">The sequence to search.</param>
        /// <param name="uniqueId">The unique ID of the step to find.</param>
        public static Step? FindStepById(this Sequence sequence, string uniqueId)
        {
            return sequence.GetAllSteps().FirstOrDefault(s => s.UniqueId == uniqueId);
        }
    }
}
