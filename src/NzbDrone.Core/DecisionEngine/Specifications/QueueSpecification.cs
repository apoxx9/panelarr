using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Queue;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class QueueSpecification : IDecisionEngineSpecification
    {
        private readonly IQueueService _queueService;
        private readonly UpgradableSpecification _upgradableSpecification;
        private readonly ICustomFormatCalculationService _formatService;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public QueueSpecification(IQueueService queueService,
                                  UpgradableSpecification upgradableSpecification,
                                  ICustomFormatCalculationService formatService,
                                  IConfigService configService,
                                  Logger logger)
        {
            _queueService = queueService;
            _upgradableSpecification = upgradableSpecification;
            _formatService = formatService;
            _configService = configService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public Decision IsSatisfiedBy(RemoteIssue subject, SearchCriteriaBase searchCriteria)
        {
            var queue = _queueService.GetQueue();
            var matchingIssue = queue.Where(q => q.RemoteIssue?.Series != null &&
                                                 q.RemoteIssue.Series.Id == subject.Series.Id &&
                                                 q.RemoteIssue.Issues.Select(e => e.Id).Intersect(subject.Issues.Select(e => e.Id)).Any())
                           .ToList();

            foreach (var queueItem in matchingIssue)
            {
                var remoteIssue = queueItem.RemoteIssue;
                var qualityProfile = subject.Series.QualityProfile.Value;

                // To avoid a race make sure it's not FailedPending (failed awaiting removal/search).
                // Failed items (already searching for a replacement) won't be part of the queue since
                // it's a copy, of the tracked download, not a reference.
                if (queueItem.TrackedDownloadState == TrackedDownloadState.DownloadFailedPending)
                {
                    continue;
                }

                _logger.Debug("Checking if existing release in queue meets cutoff. Queued quality is: {0}", remoteIssue.ParsedIssueInfo.Quality);

                var queuedItemCustomFormats = _formatService.ParseCustomFormat(remoteIssue, (long)queueItem.Size);

                if (!_upgradableSpecification.CutoffNotMet(qualityProfile,
                                                           new List<QualityModel> { remoteIssue.ParsedIssueInfo.Quality },
                                                           queuedItemCustomFormats,
                                                           subject.ParsedIssueInfo.Quality))
                {
                    return Decision.Reject("Release in queue already meets cutoff: {0}", remoteIssue.ParsedIssueInfo.Quality);
                }

                _logger.Debug("Checking if release is higher quality than queued release. Queued: {0}", remoteIssue.ParsedIssueInfo.Quality);

                if (!_upgradableSpecification.IsUpgradable(qualityProfile,
                                                           remoteIssue.ParsedIssueInfo.Quality,
                                                           queuedItemCustomFormats,
                                                           subject.ParsedIssueInfo.Quality,
                                                           subject.CustomFormats))
                {
                    return Decision.Reject("Release in queue is of equal or higher preference: {0}", remoteIssue.ParsedIssueInfo.Quality);
                }

                _logger.Debug("Checking if profiles allow upgrading. Queued: {0}", remoteIssue.ParsedIssueInfo.Quality);

                if (!_upgradableSpecification.IsUpgradeAllowed(qualityProfile,
                                                               remoteIssue.ParsedIssueInfo.Quality,
                                                               queuedItemCustomFormats,
                                                               subject.ParsedIssueInfo.Quality,
                                                               subject.CustomFormats))
                {
                    return Decision.Reject("Another release is queued and the Quality profile does not allow upgrades");
                }

                if (_upgradableSpecification.IsRevisionUpgrade(remoteIssue.ParsedIssueInfo.Quality, subject.ParsedIssueInfo.Quality))
                {
                    if (_configService.DownloadPropersAndRepacks == ProperDownloadTypes.DoNotUpgrade)
                    {
                        _logger.Debug("Auto downloading of propers is disabled");
                        return Decision.Reject("Proper downloading is disabled");
                    }
                }
            }

            return Decision.Accept();
        }
    }
}
