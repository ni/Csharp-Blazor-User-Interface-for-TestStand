namespace NationalInstruments.TestStand.WebOI.SharedDomain
{
    /// <summary>
    /// Constants used in the sequencing operations.
    /// </summary>
    public static class SequencingConstants
    {
        /// <summary>
        /// Status indicating that an execution is being initialized.
        /// </summary>
        public const string InitializingStatus = "Initializing";

        /// <summary>
        /// Status indicating that a sequence is currently running.
        /// </summary>
        public const string RunningStatus = "Running";

        /// <summary>
        /// Status indicating that a sequence is completing.
        /// </summary>
        public const string CompletingStatus = "Completing";

        /// <summary>
        /// Status indicating that a sequence is aborting.
        /// </summary>
        public const string AbortingStatus = "Aborting";

        /// <summary>
        /// Status indicating that a sequence is terminating.
        /// </summary>
        public const string TerminatingStatus = "Terminating";

        /// <summary>
        /// Status indicating that a report is being generated.
        /// </summary>
        public const string GeneratingReportStatus = "GeneratingReport";

        /// <summary>
        /// Status indicating that a sequence has been paused.
        /// </summary>
        public const string PausedStatus = "Paused";

        /// <summary>
        /// Status indicating that a sequence has passed.
        /// </summary>
        public const string PassedStatus = "Passed";

        /// <summary>
        /// Status indicating that a sequence has completed.
        /// </summary>
        public const string CompletedStatus = "Completed";

        /// <summary>
        /// Status indicating that a sequence has been terminated.
        /// </summary>
        public const string TerminatedStatus = "Terminated";

        /// <summary>
        /// Status indicating that a sequence has been aborted.
        /// </summary>
        public const string AbortedStatus = "Aborted";

        /// <summary>
        /// Status indicating that a sequence has failed.
        /// </summary>
        public const string FailedStatus = "Failed";

        /// <summary>
        /// Status indicating that an error occurred during execution.
        /// </summary>
        public const string ErrorStatus = "Error";

        /// <summary>
        /// Status indicating that a sequence has been skipped.
        /// </summary>
        public const string SkippedStatus = "Skipped";

        /// <summary>
        /// Status indicating that a sequence is done.
        /// </summary>
        public const string DoneStatus = "Done";
    }
}