using System.Collections.Generic;
using NLog;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Specifications
{
    public class CloseIssueMatchSpecification : IImportDecisionEngineSpecification<LocalEdition>
    {
        private const double _issueThreshold = 0.20;
        private readonly Logger _logger;

        public CloseIssueMatchSpecification(Logger logger)
        {
            _logger = logger;
        }

        public Decision IsSatisfiedBy(LocalEdition item, DownloadClientItem downloadClientItem)
        {
            double dist;
            string reasons;

            // strict when a new download
            if (item.NewDownload)
            {
                dist = item.Distance.NormalizedDistance();
                reasons = item.Distance.Reasons;
                if (dist > _issueThreshold)
                {
                    _logger.Debug($"Issue match is not close enough: {dist} vs {_issueThreshold} {reasons}. Skipping {item}");
                    return Decision.Reject($"Issue match is not close enough: {1 - dist:P1} vs {1 - _issueThreshold:P0} {reasons}");
                }
            }

            // otherwise importing existing files in library
            else
            {
                // get issue distance ignoring whether tracks are missing
                dist = item.Distance.NormalizedDistanceExcluding(new List<string> { "missing_tracks", "unmatched_tracks" });
                reasons = item.Distance.Reasons;
                if (dist > _issueThreshold)
                {
                    _logger.Debug($"Issue match is not close enough: {dist} vs {_issueThreshold} {reasons}. Skipping {item}");
                    return Decision.Reject($"Issue match is not close enough: {1 - dist:P1} vs {1 - _issueThreshold:P0} {reasons}");
                }
            }

            _logger.Debug($"Accepting release {item}: dist {dist} vs {_issueThreshold} {reasons}");
            return Decision.Accept();
        }
    }
}
