using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.Issues
{
    public class MoveSeriesService : IExecute<MoveSeriesCommand>, IExecute<BulkMoveSeriesCommand>
    {
        private readonly ISeriesService _seriesService;
        private readonly IBuildFileNames _filenameBuilder;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderWatchingService _rootFolderWatchingService;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public MoveSeriesService(ISeriesService seriesService,
                                 IBuildFileNames filenameBuilder,
                                 IDiskProvider diskProvider,
                                 IRootFolderWatchingService rootFolderWatchingService,
                                 IDiskTransferService diskTransferService,
                                 IEventAggregator eventAggregator,
                                 Logger logger)
        {
            _seriesService = seriesService;
            _filenameBuilder = filenameBuilder;
            _diskProvider = diskProvider;
            _rootFolderWatchingService = rootFolderWatchingService;
            _diskTransferService = diskTransferService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        private void MoveSingleSeries(Series series, string sourcePath, string destinationPath, int? index = null, int? total = null)
        {
            if (!_diskProvider.FolderExists(sourcePath))
            {
                _logger.Debug("Folder '{0}' for '{1}' does not exist, not moving.", sourcePath, series.Name);
                return;
            }

            if (index != null && total != null)
            {
                _logger.ProgressInfo("Moving {0} from '{1}' to '{2}' ({3}/{4})", series.Name, sourcePath, destinationPath, index + 1, total);
            }
            else
            {
                _logger.ProgressInfo("Moving {0} from '{1}' to '{2}'", series.Name, sourcePath, destinationPath);
            }

            if (sourcePath.PathEquals(destinationPath))
            {
                _logger.ProgressInfo("{0} is already in the specified location '{1}'.", series, destinationPath);
                return;
            }

            try
            {
                _rootFolderWatchingService.ReportFileSystemChangeBeginning(sourcePath, destinationPath);

                _diskTransferService.TransferFolder(sourcePath, destinationPath, TransferMode.Move);

                _logger.ProgressInfo("{0} moved successfully to {1}", series.Name, destinationPath);

                _eventAggregator.PublishEvent(new SeriesMovedEvent(series, sourcePath, destinationPath));
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "Unable to move series from '{0}' to '{1}'. Try moving files manually", sourcePath, destinationPath);

                RevertPath(series.Id, sourcePath);
            }
        }

        private void RevertPath(int seriesId, string path)
        {
            var series = _seriesService.GetSeries(seriesId);

            series.Path = path;
            _seriesService.UpdateSeries(series);
        }

        public void Execute(MoveSeriesCommand message)
        {
            var series = _seriesService.GetSeries(message.SeriesId);
            MoveSingleSeries(series, message.SourcePath, message.DestinationPath);
        }

        public void Execute(BulkMoveSeriesCommand message)
        {
            var seriesToMove_ = message.Series;
            var destinationRootFolder = message.DestinationRootFolder;

            _logger.ProgressInfo("Moving {0} series to '{1}'", seriesToMove_.Count, destinationRootFolder);

            for (var index = 0; index < seriesToMove_.Count; index++)
            {
                var s = seriesToMove_[index];
                var series = _seriesService.GetSeries(s.SeriesId);
                var destinationPath = Path.Combine(destinationRootFolder, _filenameBuilder.GetSeriesFolder(series));

                MoveSingleSeries(series, s.SourcePath, destinationPath, index, seriesToMove_.Count);
            }

            _logger.ProgressInfo("Finished moving {0} series to '{1}'", seriesToMove_.Count, destinationRootFolder);
        }
    }
}
