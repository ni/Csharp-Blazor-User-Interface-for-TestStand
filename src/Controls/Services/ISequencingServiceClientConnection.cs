namespace NationalInstruments.TestStand.WebOI.UI.Services
{
    /// <summary>
    /// Type managing the gRPC connection to Sequencing services.
    /// </summary>
    internal interface ISequencingServiceClientConnection
    {
        /// <summary>
        /// Connect the required services.
        /// </summary>
        Task ConnectServicesAsync();

        /// <summary>
        /// Disconnect from the services.
        /// </summary>
        void DisconnectServices();

        /// <summary>
        /// Represents the connection status to the Sequencing services.
        /// </summary>
        ConnectionStatus Status { get; set; }

        event EventHandler? OnConnectionStatusChanged;
    }

    internal enum ConnectionStatus
    {
        Connecting,
        Connected,
        Disconnected
    }
}
