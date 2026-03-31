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
        private readonly ISeriesService _seriesService;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IIssueAddedService _bookAddedService;
        private readonly Logger _logger;

        public SeriesScannedHandler(IIssueMonitoredService bookMonitoredService,
                                    ISeriesService seriesService,
                                    IManageCommandQueue commandQueueManager,
                                    IIssueAddedService bookAddedService,
                                    Logger logger)
        {
            _issueMonitoredService = bookMonitoredService;
            _seriesService = seriesService;
            _commandQueueManager = commandQueueManager;
            _bookAddedService = bookAddedService;
            _logger = logger;
        }

        private void HandleScanEvents(Series series)
        {
            if (series.AddOptions != null)
            {
                _logger.Info("[{0}] was recently added, performing post-add actions", series.Name);
                _issueMonitoredService.SetIssueMonitoredStatus(series, series.AddOptions);

                if (series.AddOptions.SearchForMissingIssues)
                {
                    _commandQueueManager.Push(new MissingIssueSearchCommand(series.Id));
                }

                series.AddOptions = null;
                _seriesService.RemoveAddOptions(series);
            }

            _bookAddedService.SearchForRecentlyAdded(series.Id);
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
