using Microsoft.AspNetCore.Components;

namespace NationalInstruments.TestStand.WebOI.UI.Components
{
    /// <summary>
    /// Represents a UI component that displays an icon corresponding to the current execution status of an operation.
    /// </summary>
    /// <remarks>The <see cref="ExecutionStatusIcon"/> component is designed to visually indicate the status
    /// of an operation based on the value of the <see cref="Status"/> parameter. The caller is responsible for
    /// providing a valid status value, which the component uses to determine the appropriate icon to display.</remarks>
    public sealed partial class ExecutionStatusIcon : ComponentBase
    {
        /// <summary>
        /// Gets or sets the current execution status of the operation.
        /// </summary>
        [Parameter]
        public string Status { get; set; } = string.Empty;
    }
}
