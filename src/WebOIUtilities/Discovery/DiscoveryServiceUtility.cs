using NationalInstruments.MeasurementLink.Discovery.V1;

namespace NationalInstruments.TestStand.WebOI.Utilities.Discovery
{
    /// <summary>
    /// Utility for using the <see cref="DiscoveryService"/>.
    /// </summary>
    /// <param name="logger">A logger instance.</param>
    /// <param name="discoveryClient">Discovery service client functionality</param>
    public sealed class DiscoveryServiceUtility(
        ILogger<DiscoveryServiceUtility> logger,
        IDiscoveryClient discoveryClient) : IDiscoveryServiceUtility
    {
        /// <summary>
        /// Use the <see cref="DiscoveryService"/> to resolve the location of a service given a provided interface.
        /// </summary>
        /// <param name="providedInterface">The interface to try to resolve.</param>
        /// <returns>A URI with the location of the service, or null if resolving failed.</returns>
        public async Task<string?> ResolveServiceAsync(string providedInterface)
        {
            ResolveServiceRequest request = new() { ProvidedInterface = providedInterface };
            ServiceLocation? response = await discoveryClient.ResolveServiceAsync(request);
            if (response is null)
            {
                logger.LogError("DiscoveryService could not resolve service with provided interface: {Name}", providedInterface);
                return null;
            }
            if (!int.TryParse(response.InsecurePort, out int port))
            {
                logger.LogError("Failed to parse insecure port when resolving service with provided interface: {Name}", providedInterface);
                return null;
            }
            UriBuilder uriBuilder = new(Uri.UriSchemeHttp, response.Location, port);
            string uri = uriBuilder.Uri.ToString();
            logger.LogInformation("Successfully resolved {Interface} to {Uri}.", providedInterface, uri);
            return uri;
        }
    }
}
