using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.IssueImport.Specifications
{
    public class UpgradeSpecification : IImportDecisionEngineSpecification<LocalIssue>
    {
        private readonly IConfigService _configService;
        private readonly ICustomFormatCalculationService _customFormatCalculationService;
        private readonly Logger _logger;

        public UpgradeSpecification(IConfigService configService,
                                    ICustomFormatCalculationService customFormatCalculationService,
                                    Logger logger)
        {
            _configService = configService;
            _customFormatCalculationService = customFormatCalculationService;
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalIssue item, DownloadClientItem downloadClientItem)
        {
            var files = item.Issue?.ComicFiles?.Value;
            if (files == null || !files.Any())
            {
                // No existing issues, skip.  This guards against new allSeries not having a QualityProfile.
                return Decision.Accept();
            }

            var downloadPropersAndRepacks = _configService.DownloadPropersAndRepacks;
            var qualityComparer = new QualityModelComparer(item.Series.QualityProfile);

            foreach (var comicFile in files)
            {
                var qualityCompare = qualityComparer.Compare(item.Quality.Quality, comicFile.Quality.Quality);

                if (qualityCompare < 0)
                {
                    _logger.Debug("This file isn't a quality upgrade for all issues. Skipping {0}", item.Path);
                    return Decision.Reject("Not an upgrade for existing issue file(s)");
                }

                if (qualityCompare == 0 && downloadPropersAndRepacks != ProperDownloadTypes.DoNotPrefer &&
                    item.Quality.Revision.CompareTo(comicFile.Quality.Revision) < 0)
                {
                    _logger.Debug("This file isn't a quality upgrade for all issues. Skipping {0}", item.Path);
                    return Decision.Reject("Not an upgrade for existing issue file(s)");
                }
            }

            return Decision.Accept();
        }
    }
}
