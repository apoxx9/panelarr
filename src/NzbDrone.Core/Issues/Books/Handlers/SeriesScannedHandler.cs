using NLog;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public class SeriesScannedHandler : IHandle<SeriesScannedEvent>,
                                        IHandle<SeriesScanSkippedEvent>
    {
        private readonly IIssueMonitoredService _issueMonitoredService;
        private readonly ISeriesService _authorService;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IIssueAddedService _bookAddedService;
        private readonly Logger _logger;

        public SeriesScannedHandler(IIssueMonitoredService bookMonitoredService,
                                    ISeriesService authorService,
                                    IManageCommandQueue commandQueueManager,
                                    IIssueAddedService bookAddedService,
                                    Logger logger)
        {
            _issueMonitoredService = bookMonitoredService;
            _authorService = authorService;
            _commandQueueManager = commandQueueManager;
            _bookAddedService = bookAddedService;
            _logger = logger;
        }

        private void HandleScanEvents(Series author)
        {
            if (author.AddOptions != null)
            {
                _logger.Info("[{0}] was recently added, performing post-add actions", author.Name);
                _issueMonitoredService.SetIssueMonitoredStatus(author, author.AddOptions);

                if (author.AddOptions.SearchForMissingIssues)
                {
                    _commandQueueManager.Push(new MissingIssueSearchCommand(author.Id));
                }

                author.AddOptions = null;
                _authorService.RemoveAddOptions(author);
            }

            _bookAddedService.SearchForRecentlyAdded(author.Id);
        }

        public void Handle(SeriesScannedEvent message)
        {
            HandleScanEvents(message.Series);
        }

        public void Handle(SeriesScanSkippedEvent message)
        {
            HandleScanEvents(message.Series);
        }
    }
}
