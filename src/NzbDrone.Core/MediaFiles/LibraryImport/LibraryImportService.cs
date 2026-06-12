using System;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.LibraryImport
{
    // Adds the confirmed proposals sequentially: each AddSeries fetches the
    // series from the metadata provider (throttled by the provider's own rate
    // limiter) and the SeriesAdded handler refreshes and rescans its folder,
    // which maps the files — exactly via tagged ids where present. The series
    // path is the EXISTING folder; nothing on disk moves.
    public class LibraryImportService : IExecute<LibraryImportCommand>
    {
        private readonly IAddSeriesService _addSeriesService;
        private readonly ISeriesService _seriesService;
        private readonly Logger _logger;

        public LibraryImportService(IAddSeriesService addSeriesService,
                                    ISeriesService seriesService,
                                    Logger logger)
        {
            _addSeriesService = addSeriesService;
            _seriesService = seriesService;
            _logger = logger;
        }

        public void Execute(LibraryImportCommand message)
        {
            if (message.Series == null || message.Series.Count == 0)
            {
                _logger.Warn("Library import command received no series");
                return;
            }

            var monitorNewItems = Enum.TryParse<NewItemMonitorTypes>(message.MonitorNewItems, true, out var parsed)
                ? parsed
                : NewItemMonitorTypes.All;

            var added = 0;
            var skipped = 0;
            var failed = 0;
            var total = message.Series.Count;

            foreach (var item in message.Series)
            {
                try
                {
                    if (item.ForeignSeriesId.IsNullOrWhiteSpace() || item.Folder.IsNullOrWhiteSpace())
                    {
                        _logger.Warn("Skipping library import entry with missing id or folder");
                        skipped++;
                        continue;
                    }

                    if (_seriesService.FindById(item.ForeignSeriesId) != null)
                    {
                        _logger.Debug("Series {0} is already in the library, skipping", item.ForeignSeriesId);
                        skipped++;
                        continue;
                    }

                    var series = new Series
                    {
                        Metadata = new SeriesMetadata { ForeignSeriesId = item.ForeignSeriesId },
                        Path = item.Folder,
                        QualityProfileId = message.QualityProfileId,
                        Monitored = message.Monitored,
                        MonitorNewItems = monitorNewItems,
                        AddOptions = new AddSeriesOptions
                        {
                            SearchForMissingIssues = false,
                            Monitor = message.Monitored ? MonitorTypes.All : MonitorTypes.None
                        }
                    };

                    _addSeriesService.AddSeries(series);
                    added++;

                    _logger.ProgressInfo("Library import: {0}/{1} — added {2}", added + skipped + failed, total, item.ForeignSeriesId);
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.Error(ex, "Library import failed for {0} ({1})", item.ForeignSeriesId, item.Folder);
                }
            }

            _logger.ProgressInfo("Library import finished: {0} added, {1} skipped, {2} failed of {3}", added, skipped, failed, total);
        }
    }
}
