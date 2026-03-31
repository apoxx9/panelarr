using System.Linq;
using NLog;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class UpgradeAllowedSpecification : IDecisionEngineSpecification
    {
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly ICustomFormatCalculationService _formatService;
        private readonly Logger _logger;

        public UpgradeAllowedSpecification(UpgradableSpecification upgradableSpecification,
                                           Logger logger,
                                           ICustomFormatCalculationService formatService)
        {
            _upgradableSpecification = upgradableSpecification;
            _formatService = formatService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteIssue subject, SearchCriteriaBase searchCriteria)
        {
            var qualityProfile = subject.Series.QualityProfile.Value;

            foreach (var file in subject.Issues.SelectMany(b => b.ComicFiles.Value))
            {
                if (file == null)
                {
                    _logger.Debug("File is no longer available, skipping this file.");
                    continue;
                }

                var fileCustomFormats = _formatService.ParseCustomFormat(file, subject.Series);
                _logger.Debug("Comparing file quality with report. Existing files contain {0}", file.Quality);

                if (!_upgradableSpecification.IsUpgradeAllowed(qualityProfile,
                                                               file.Quality,
                                                               fileCustomFormats,
                                                               subject.ParsedIssueInfo.Quality,
                                                               subject.CustomFormats))
                {
                    _logger.Debug("Upgrading is not allowed by the quality profile");

                    return Decision.Reject("Existing files and the Quality profile does not allow upgrades");
                }
            }

            return Decision.Accept();
        }
    }
}
