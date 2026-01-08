using NationalInstruments.Sequencing.V2;

namespace NationalInstruments.TestStand.BlazorOI.UI.Services.Events
{
    /// <summary>
    /// Encapsulates information about the previous executed step and next executing step.
    /// </summary>
    /// <param name="executionId">The unique identifier for the execution.</param>
    /// <param name="previousStep">Information about the previously executed step.</param>
    /// <param name="nextStep">Information about the next executing step.</param>
    internal sealed class ExecutionTracingEventArgs(int executionId, StepLocation? previousStep, StepLocation? nextStep) : EventArgs
    {
        public int ExecutionId { get; } = executionId;

        public StepLocation? PreviousStep { get; } = previousStep;

        public StepLocation? NextStep { get; } = nextStep;
    }

    /// <summary>
    /// Determines the location of a step within a sequence.
    /// </summary>
    /// <param name="step">The step object.</param>
    /// <param name="stepIndex">The 0-based index of the step relative to the entire steps collection (including all step groups) in the sequence.</param>
    internal sealed class StepLocation(Step step, int stepIndex)
    {
        public Step Step { get; } = step;
        public int StepIndex { get; } = stepIndex;
    }
}