using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Specifications
{
    public class AlreadyImportedSpecification : IImportDecisionEngineSpecification<LocalEdition>
    {
        private readonly IHistoryService _historyService;
        private readonly Logger _logger;

        public AlreadyImportedSpecification(IHistoryService historyService,
                                            Logger logger)
        {
            _historyService = historyService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Database;

        public Decision IsSatisfiedBy(LocalEdition localIssueRelease, DownloadClientItem downloadClientItem)
        {
            if (downloadClientItem == null)
            {
                _logger.Debug("No download client information is available, skipping");
                return Decision.Accept();
            }

            var issueRelease = localIssueRelease.Issue;

            if ((!issueRelease?.ComicFiles?.Value?.Any()) ?? true)
            {
                _logger.Debug("Skipping already imported check for issue without files");
                return Decision.Accept();
            }

            var issueHistory = _historyService.GetByIssue(issueRelease.Id, null);
            var lastImported = issueHistory.FirstOrDefault(h => h.EventType == EntityHistoryEventType.ComicFileImported);
            var lastGrabbed = issueHistory.FirstOrDefault(h => h.EventType == EntityHistoryEventType.Grabbed);

            if (lastImported == null)
            {
                _logger.Trace("Issue file has not been imported");
                return Decision.Accept();
            }

            if (lastGrabbed != null && lastGrabbed.Date.After(lastImported.Date))
            {
                _logger.Trace("Issue file was grabbed again after importing");
                return Decision.Accept();
            }

            if (lastImported.DownloadId == downloadClientItem.DownloadId)
            {
                _logger.Debug("Issue previously imported at {0}", lastImported.Date);
                return Decision.Reject("Issue already imported at {0}", lastImported.Date);
            }

            return Decision.Accept();
        }
    }
}
