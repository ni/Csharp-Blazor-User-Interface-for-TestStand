using System.IO.Abstractions;

namespace NationalInstruments.TestStand.WebOI.Utilities.DevelopmentFeatures
{
    internal sealed class DevelopmentFeaturesService(IFileSystem fileSystem, ILogger<DevelopmentFeaturesService> logger) : IDevelopmentFeaturesService
    {
        internal const string ShowDevelopmentFeaturesFileName = "show-dev-features";
        private bool? _showDeveloperFeatures;

        public bool ShowDevelopmentFeatures
        {
            get
            {
                if (_showDeveloperFeatures is null)
                {
                    _showDeveloperFeatures = fileSystem.File.Exists(ShowDevelopmentFeaturesFileName);
                    if (_showDeveloperFeatures.Value)
                    {
                        logger.LogInformation(
                            "Enabling development features due to beacon file found at {Path}.",
                            fileSystem.Path.GetFullPath(ShowDevelopmentFeaturesFileName));
                    }
                }
                return _showDeveloperFeatures.Value;
            }

            internal set => _showDeveloperFeatures = value;
        }
    }
}
