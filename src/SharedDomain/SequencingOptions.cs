namespace NationalInstruments.TestStand.BlazorOI.SharedDomain
{
    /// <summary>
    /// The step group object.
    /// </summary>
    public enum StepGroup
    {
        /// <summary>
        /// The setup group steps.
        /// </summary>
        Setup = 0,

        /// <summary>
        /// The main group steps.
        /// </summary>
        Main = 1,

        /// <summary>
        /// The cleanup group steps.
        /// </summary>
        Cleanup = 2
    }

    /// <summary>
    /// Represents the different states of a breakpoint icon.
    /// </summary>
    /// <remarks>This enumeration is typically used to indicate the visual state of a breakpoint in the UI.</remarks>
    public enum BreakpointIcon
    {
        /// <summary>
        /// Indicates that the breakpoint state is not set.
        /// </summary>
        Unset = 0,

        /// <summary>
        /// Indicates that the breakpoint is set and enabled.
        /// </summary>
        Enabled = 1,

        /// <summary>
        /// Indicates that the breakpoint is set but disabled.
        /// </summary>
        Disabled = 2,

        /// <summary>
        /// Indicates that the breakpoint is set and conditional.
        /// </summary>
        Conditional = 3
    }
}