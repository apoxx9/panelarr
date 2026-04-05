using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    public class RedownloadFailedDownloadService : IHandle<DownloadFailedEvent>
    {
        private readonly IConfigService _configService;
        private readonly IIssueService _issueService;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public RedownloadFailedDownloadService(IConfigService configService,
                                               IIssueService issueService,
                                               IManageCommandQueue commandQueueManager,
                                               Logger logger)
        {
            _configService = configService;
            _issueService = issueService;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        [EventHandleOrder(EventHandleOrder.Last)]
        public void Handle(DownloadFailedEvent message)
        {
            if (message.SkipRedownload)
            {
                _logger.Debug("Skip redownloading requested by user");
                return;
            }

            if (!_configService.AutoRedownloadFailed)
            {
                _logger.Debug("Auto redownloading failed issues is disabled");
                return;
            }

            if (message.ReleaseSource == ReleaseSourceType.InteractiveSearch && !_configService.AutoRedownloadFailedFromInteractiveSearch)
            {
                _logger.Debug("Auto redownloading failed issues from interactive search is disabled");
                return;
            }

            if (message.IssueIds.Count == 1)
            {
                _logger.Debug("Failed download only contains one issue, searching again");

                _commandQueueManager.Push(new IssueSearchCommand(message.IssueIds));

                return;
            }

            var issuesInSeries = _issueService.GetIssuesBySeries(message.SeriesId);

            if (message.IssueIds.Count == issuesInSeries.Count)
            {
                _logger.Debug("Failed download was entire series, searching again");

                _commandQueueManager.Push(new SeriesSearchCommand
                {
                    SeriesId = message.SeriesId
                });

                return;
            }

            _logger.Debug("Failed download contains multiple issues, searching again");

            _commandQueueManager.Push(new IssueSearchCommand(message.IssueIds));
        }
    }
}
