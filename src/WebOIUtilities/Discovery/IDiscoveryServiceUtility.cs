namespace NationalInstruments.TestStand.WebOI.Utilities.Discovery
{
    /// <summary>
    /// Utility used to resolve services.
    /// </summary>
    public interface IDiscoveryServiceUtility
    {
        /// <summary>
        /// Use to resolve the location of a service given a provided interface
        /// </summary>
        /// <param name="providedInterface">The interface to try to resolve.</param>
        /// <returns>A URI with the location of the service, or null if resolving failed.</returns>
        Task<string?> ResolveServiceAsync(string providedInterface);
    }
}
