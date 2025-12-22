namespace NationalInstruments.TestStand.WebOI.Utilities.DevelopmentFeatures
{
    /// <summary>
    /// Provides information about developer-only features and whether they should be shown.
    /// </summary>
    public interface IDevelopmentFeaturesService
    {
        /// <summary>
        /// Whether to show developer-only features.
        /// </summary>
        bool ShowDevelopmentFeatures { get; }
    }
}
