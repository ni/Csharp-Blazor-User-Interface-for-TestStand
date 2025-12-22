namespace NationalInstruments.TestStand.WebOI.Utilities.Discovery
{
    /// <summary>
    /// Constants used for services.
    /// </summary>
    public static class ServiceConstants
    {
        /// <summary>
        /// Service interface to resolve the Sequencing Service.
        /// </summary>
        /// <remarks>Needs to be kept in sync with the interface value in .\Armstrong\Src\SequencingServiceCore\SequencingServiceGlobal.serviceconfig </remarks>
        public const string SequencingServiceProvidedInterface = "ni.sequencing.v2.SequencingService.Global";
    }
}
