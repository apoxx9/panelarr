using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles.LibraryImport
{
    // Executes the staging import: per confirmed proposal, ensure the series
    // exists (created under the target root, refreshed synchronously so its
    // issues are available for matching), then run the staging folder's files
    // through the regular import decision engine and transfer them into the
    // library. Move (copy+verify+delete) by default; copy when the source
    // must be preserved. Rejected files stay in the staging folder.
    public class StagingImportService : IExecute<StagingImportCommand>
    {
        private readonly IAddSeriesService _addSeriesService;
        private readonly ISeriesService _seriesService;
        private readonly IRefreshSeriesService _refreshSeriesService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IDiskScanService _diskScanService;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly IImportApprovedIssues _importApprovedIssues;
        private readonly Logger _logger;

        public StagingImportService(IAddSeriesService addSeriesService,
                                    ISeriesService seriesService,
                                    IRefreshSeriesService refreshSeriesService,
                                    IRootFolderService rootFolderService,
                                    IDiskScanService diskScanService,
                                    IMakeImportDecision importDecisionMaker,
                                    IImportApprovedIssues importApprovedIssues,
                                    Logger logger)
        {
            _addSeriesService = addSeriesService;
            _seriesService = seriesService;
            _refreshSeriesService = refreshSeriesService;
            _rootFolderService = rootFolderService;
            _diskScanService = diskScanService;
            _importDecisionMaker = importDecisionMaker;
            _importApprovedIssues = importApprovedIssues;
            _logger = logger;
        }

        public void Execute(StagingImportCommand message)
        {
            if (message.Series == null || message.Series.Count == 0)
            {
                _logger.Warn("Staging import command received no series");
                return;
            }

            if (message.RootFolderPath.IsNullOrWhiteSpace() ||
                !_rootFolderService.All().Any(r => r.Path.PathEquals(message.RootFolderPath)))
            {
                // The import pipeline refuses destinations outside a root
                // folder, so fail fast with a clear message.
                throw new ArgumentException($"Target root folder '{message.RootFolderPath}' is not a configured root folder");
            }

            var monitorNewItems = Enum.TryParse<NewItemMonitorTypes>(message.MonitorNewItems, true, out var parsed)
                ? parsed
                : NewItemMonitorTypes.All;

            var importMode = message.KeepSourceFiles ? ImportMode.Copy : ImportMode.Move;

            var imported = 0;
            var rejected = 0;
            var failedSeries = 0;
            var processed = 0;
            var total = message.Series.Count;

            foreach (var item in message.Series)
            {
                processed++;

                try
                {
                    if (item.ForeignSeriesId.IsNullOrWhiteSpace() || item.Folder.IsNullOrWhiteSpace())
                    {
                        _logger.Warn("Skipping staging import entry with missing id or folder");
                        failedSeries++;
                        continue;
                    }

                    var series = _seriesService.FindById(item.ForeignSeriesId);

                    if (series == null)
                    {
                        series = _addSeriesService.AddSeries(new Series
                        {
                            Metadata = new SeriesMetadata { ForeignSeriesId = item.ForeignSeriesId },
                            RootFolderPath = message.RootFolderPath,
                            QualityProfileId = message.QualityProfileId,
                            Monitored = message.Monitored,
                            MonitorNewItems = monitorNewItems,
                            AddOptions = new AddSeriesOptions
                            {
                                SearchForMissingIssues = false,
                                Monitor = message.Monitored ? MonitorTypes.All : MonitorTypes.None
                            }
                        }, doRefresh: false);

                        // Synchronous refresh: the import decisions below need
                        // the series' issues in the database.
                        _refreshSeriesService.RefreshSeries(new List<int> { series.Id }, true, CommandTrigger.Manual);
                        series = _seriesService.GetSeries(series.Id);
                    }

                    var files = _diskScanService.GetComicFiles(item.Folder, false).ToList();

                    if (files.Empty())
                    {
                        _logger.Warn("Staging folder {0} has no comic files", item.Folder);
                        continue;
                    }

                    var decisions = _importDecisionMaker.GetImportDecisions(files,
                        new IdentificationOverrides { Series = series },
                        null,
                        new ImportDecisionMakerConfig
                        {
                            Filter = FilterFilesType.None,
                            NewDownload = true,
                            SingleRelease = false,
                            IncludeExisting = false,
                            AddNewSeries = false
                        });

                    var results = _importApprovedIssues.Import(decisions, false, null, importMode);

                    var importedHere = results.Count(r => r.Result == ImportResultType.Imported);
                    var rejectedHere = files.Count - importedHere;

                    imported += importedHere;
                    rejected += rejectedHere;

                    foreach (var decision in decisions.Where(d => !d.Approved))
                    {
                        _logger.Debug("Staging import rejected {0}: {1}", decision.Item.Path, string.Join(", ", decision.Rejections.Select(r => r.Reason)));
                    }

                    _logger.ProgressInfo("Staging import: {0}/{1} — {2}: {3} imported, {4} left in staging", processed, total, series.Name, importedHere, rejectedHere);
                }
                catch (Exception ex)
                {
                    failedSeries++;
                    _logger.Error(ex, "Staging import failed for {0} ({1})", item.ForeignSeriesId, item.Folder);
                }
            }

            _logger.ProgressInfo("Staging import finished: {0} files imported, {1} left in staging, {2} of {3} series failed", imported, rejected, failedSeries, total);
        }
    }
}
