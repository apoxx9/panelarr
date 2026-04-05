using NLog;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues.Handlers
{
    public class AutoUnmonitorAfterDownloadHandler : IHandle<IssueImportedEvent>
    {
        private readonly IIssueService _issueService;
        private readonly Logger _logger;

        public AutoUnmonitorAfterDownloadHandler(IIssueService issueService, Logger logger)
        {
            _issueService = issueService;
            _logger = logger;
        }

        public void Handle(IssueImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return;
            }

            var series = message.Series;

            if (series == null || !series.AutoUnmonitorAfterDownload)
            {
                return;
            }

            var issue = message.Issue;

            if (issue == null || !issue.Monitored)
            {
                return;
            }

            _logger.Debug("Auto-unmonitoring issue {0} after download (Series: {1})", issue.Title, series.Name);

            issue.Monitored = false;
            _issueService.UpdateIssue(issue);
        }
    }
}
