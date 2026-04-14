using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists
{
    public class ImportListSyncService : IExecute<ImportListSyncCommand>
    {
        private readonly IImportListFactory _importListFactory;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly IFetchAndParseImportList _listFetcherAndParser;
        private readonly IProvideIssueInfo _issueInfoProxy;
        private readonly ISearchForNewIssue _searchProxy;
        private readonly ISearchForNewSeries _searchSeriesProxy;
        private readonly ISeriesService _seriesService;
        private readonly IIssueService _issueService;
        private readonly IAddSeriesService _addSeriesService;
        private readonly IAddIssueService _addIssueService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public ImportListSyncService(IImportListFactory importListFactory,
                                     IImportListExclusionService importListExclusionService,
                                     IFetchAndParseImportList listFetcherAndParser,
                                     IProvideIssueInfo issueInfoProxy,
                                     ISearchForNewIssue searchProxy,
                                     ISearchForNewSeries searchSeriesProxy,
                                     ISeriesService seriesService,
                                     IIssueService issueService,
                                     IAddSeriesService addSeriesService,
                                     IAddIssueService addIssueService,
                                     IEventAggregator eventAggregator,
                                     IManageCommandQueue commandQueueManager,
                                     Logger logger)
        {
            _importListFactory = importListFactory;
            _importListExclusionService = importListExclusionService;
            _listFetcherAndParser = listFetcherAndParser;
            _issueInfoProxy = issueInfoProxy;
            _searchProxy = searchProxy;
            _searchSeriesProxy = searchSeriesProxy;
            _seriesService = seriesService;
            _issueService = issueService;
            _addSeriesService = addSeriesService;
            _addIssueService = addIssueService;
            _eventAggregator = eventAggregator;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        private List<Issue> SyncAll()
        {
            if (_importListFactory.AutomaticAddEnabled().Empty())
            {
                _logger.Debug("No import lists with automatic add enabled");

                return new List<Issue>();
            }

            _logger.ProgressInfo("Starting Import List Sync");

            var listItems = _listFetcherAndParser.Fetch().ToList();

            return ProcessListItems(listItems);
        }

        private List<Issue> SyncList(ImportListDefinition definition)
        {
            _logger.ProgressInfo($"Starting Import List Refresh for List {definition.Name}");

            var listItems = _listFetcherAndParser.FetchSingleList(definition).ToList();

            return ProcessListItems(listItems);
        }

        private List<Issue> ProcessListItems(List<ImportListItemInfo> items)
        {
            var processed = new List<Issue>();
            var seriesToAdd = new List<Series>();
            var issuesToAdd = new List<Issue>();

            if (items.Count == 0)
            {
                _logger.ProgressInfo("No list items to process");

                return new List<Issue>();
            }

            _logger.ProgressInfo("Processing {0} list items", items.Count);

            var reportNumber = 1;

            var listExclusions = _importListExclusionService.All();

            foreach (var report in items)
            {
                _logger.ProgressTrace("Processing list item {0}/{1}", reportNumber, items.Count);

                reportNumber++;

                var importList = _importListFactory.Get(report.ImportListId);

                if (report.Issue.IsNotNullOrWhiteSpace() || report.ForeignEditionId.IsNotNullOrWhiteSpace())
                {
                    if (report.ForeignEditionId.IsNullOrWhiteSpace() || report.ForeignSeriesId.IsNullOrWhiteSpace() || report.ForeignIssueId.IsNullOrWhiteSpace())
                    {
                        MapIssueReport(report);
                    }

                    ProcessIssueReport(importList, report, listExclusions, issuesToAdd, seriesToAdd);
                }
                else if (report.Series.IsNotNullOrWhiteSpace() || report.ForeignSeriesId.IsNotNullOrWhiteSpace())
                {
                    if (report.ForeignSeriesId.IsNullOrWhiteSpace())
                    {
                        MapSeriesReport(report);
                    }

                    ProcessSeriesReport(importList, report, listExclusions, seriesToAdd);
                }
            }

            var addedSeries = _addSeriesService.AddSeries(seriesToAdd, false);
            var addedIssues = _addIssueService.AddIssues(issuesToAdd, false);

            var message = string.Format($"Import List Sync Completed. Items found: {items.Count}, Series added: {seriesToAdd.Count}, Issues added: {issuesToAdd.Count}");

            _logger.ProgressInfo(message);

            var toRefresh = addedSeries.Select(x => x.Id).Concat(addedIssues.Select(x => x.Series.Value.Id)).Distinct().ToList();
            if (toRefresh.Any())
            {
                _commandQueueManager.Push(new BulkRefreshSeriesCommand(toRefresh, true));
            }

            return processed;
        }

        private void MapIssueReport(ImportListItemInfo report)
        {
            if (report.ForeignSeriesId.IsNotNullOrWhiteSpace() && report.ForeignIssueId.IsNotNullOrWhiteSpace())
            {
                return;
            }

            if (report.ForeignIssueId.IsNotNullOrWhiteSpace())
            {
                try
                {
                    var mappedIssue = _issueInfoProxy.GetIssueInfo(report.ForeignIssueId);

                    report.ForeignIssueId = mappedIssue.Item2.ForeignIssueId;
                    report.Issue = mappedIssue.Item2.Title;
                    report.ForeignSeriesId = mappedIssue.Item3.First().ForeignSeriesId;
                }
                catch (IssueNotFoundException)
                {
                    _logger.Debug($"Nothing found for issue [{report.ForeignIssueId}]");
                    report.ForeignIssueId = null;
                }
            }
            else if (report.ForeignEditionId.IsNotNullOrWhiteSpace())
            {
                try
                {
                    var mappedIssue = _issueInfoProxy.GetIssueInfo(report.ForeignEditionId);

                    _logger.Trace($"Mapped {report.ForeignEditionId} to [{mappedIssue.Item2.ForeignIssueId}] {mappedIssue.Item2.Title}");

                    report.ForeignIssueId = mappedIssue.Item2.ForeignIssueId;
                    report.Issue = mappedIssue.Item2.Title;

                    if (mappedIssue.Item3 != null && mappedIssue.Item3.Any())
                    {
                        report.Series ??= mappedIssue.Item3.First().Name;
                        report.ForeignSeriesId ??= mappedIssue.Item3.First().ForeignSeriesId;
                    }
                }
                catch (IssueNotFoundException)
                {
                    _logger.Debug($"Nothing found for edition [{report.ForeignEditionId}]");
                    report.ForeignEditionId = null;
                }
            }
            else
            {
                var searchTerm = $"{report.Issue} {report.Series}";
                var mappedIssue = _searchProxy.SearchForNewIssue(searchTerm, null, false).FirstOrDefault();

                if (mappedIssue == null)
                {
                    _logger.Trace($"Nothing found for {report.Series} - {report.Issue}");
                    return;
                }

                _logger.Trace($"Mapped Issue {report.Issue} by Series {report.Series} to [{mappedIssue.ForeignIssueId}] {mappedIssue.Title}");

                report.ForeignIssueId = mappedIssue.ForeignIssueId;
                report.Issue = mappedIssue.Title;
                report.Series ??= mappedIssue.SeriesMetadata?.Value?.Name;
                report.ForeignSeriesId ??= mappedIssue.SeriesMetadata?.Value?.ForeignSeriesId;
            }
        }

        private void ProcessIssueReport(ImportListDefinition importList, ImportListItemInfo report, List<ImportListExclusion> listExclusions, List<Issue> issuesToAdd, List<Series> seriesToAdd)
        {
            // Check to see if issue in DB
            var existingIssue = _issueService.FindById(report.ForeignIssueId);

            // Check to see if issue excluded
            var excludedIssue = listExclusions.SingleOrDefault(s => s.ForeignId == report.ForeignIssueId);

            // Check to see if series excluded
            var excludedSeries = listExclusions.SingleOrDefault(s => s.ForeignId == report.ForeignSeriesId);

            if (excludedIssue != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.ForeignEditionId, report.Issue);
                return;
            }

            if (excludedSeries != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion for parent series", report.ForeignEditionId, report.Issue);
                return;
            }

            if (existingIssue != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Issue Exists in DB.  Ensuring Issue and Series monitored.", report.ForeignEditionId, report.Issue);

                if (importList.ShouldMonitorExisting && importList.ShouldMonitor != ImportListMonitorType.None)
                {
                    if (!existingIssue.Monitored)
                    {
                        _issueService.SetIssueMonitored(existingIssue.Id, true);

                        if (importList.ShouldMonitor == ImportListMonitorType.SpecificIssue)
                        {
                            _commandQueueManager.Push(new IssueSearchCommand(new List<int> { existingIssue.Id }));
                        }
                    }

                    var existingSeries = existingIssue.Series.Value;
                    var doSearch = false;

                    if (importList.ShouldMonitor == ImportListMonitorType.EntireSeries)
                    {
                        if (existingSeries.Issues.Value.Any(x => !x.Monitored))
                        {
                            doSearch = true;
                            _issueService.SetMonitored(existingSeries.Issues.Value.Select(x => x.Id), true);
                        }
                    }

                    if (!existingSeries.Monitored)
                    {
                        doSearch = true;
                        existingSeries.Monitored = true;
                        _seriesService.UpdateSeries(existingSeries);
                    }

                    if (doSearch)
                    {
                        _commandQueueManager.Push(new MissingIssueSearchCommand(existingSeries.Id));
                    }
                }

                return;
            }

            // Append Issue if not already in DB or already on add list
            if (issuesToAdd.All(s => s.ForeignIssueId != report.ForeignIssueId))
            {
                var monitored = importList.ShouldMonitor != ImportListMonitorType.None;

                var toAddSeries = new Series
                {
                    Monitored = monitored,
                    MonitorNewItems = importList.MonitorNewItems,
                    RootFolderPath = importList.RootFolderPath,
                    QualityProfileId = importList.ProfileId,
                    Tags = importList.Tags,
                    AddOptions = new AddSeriesOptions
                    {
                        SearchForMissingIssues = importList.ShouldSearch,
                        Monitored = monitored,
                        Monitor = monitored ? MonitorTypes.All : MonitorTypes.None
                    }
                };

                if (report.ForeignSeriesId != null && report.Series != null)
                {
                    toAddSeries = ProcessSeriesReport(importList, report, listExclusions, seriesToAdd);
                }

                var toAdd = new Issue
                {
                    ForeignIssueId = report.ForeignIssueId,
                    Monitored = monitored,
                    Series = toAddSeries,
                    AddOptions = new AddIssueOptions
                    {
                        // Only search for new issue for existing allSeries
                        // New series searches are triggered by SearchForMissingIssues
                        SearchForNewIssue = importList.ShouldSearch && toAddSeries.Id > 0
                    }
                };

                if (importList.ShouldMonitor == ImportListMonitorType.SpecificIssue && toAddSeries.AddOptions != null)
                {
                    Debug.Assert(toAddSeries.Id == 0, "new series added but ID is not 0");
                    toAddSeries.AddOptions.IssuesToMonitor.Add(toAdd.ForeignIssueId);
                }

                issuesToAdd.Add(toAdd);
            }
        }

        private void MapSeriesReport(ImportListItemInfo report)
        {
            var mappedSeries = _searchSeriesProxy.SearchForNewSeries(report.Series).FirstOrDefault();

            if (mappedSeries == null)
            {
                _logger.Trace($"Nothing found for {report.Series}");
                return;
            }

            _logger.Trace($"Mapped {report.Series} to [{mappedSeries.Name}]");

            report.Series = mappedSeries.Name;
            report.ForeignSeriesId = mappedSeries.ForeignSeriesId;
        }

        private Series ProcessSeriesReport(ImportListDefinition importList, ImportListItemInfo report, List<ImportListExclusion> listExclusions, List<Series> seriesToAdd)
        {
            if (report.ForeignSeriesId == null)
            {
                return null;
            }

            // Check to see if series in DB
            var existingSeries = _seriesService.FindById(report.ForeignSeriesId);

            // Check to see if series excluded
            var excludedSeries = listExclusions.SingleOrDefault(s => s.ForeignId == report.ForeignSeriesId);

            // Check to see if series in import
            var existingImportSeries = seriesToAdd.Find(i => i.ForeignSeriesId == report.ForeignSeriesId);

            if (excludedSeries != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.ForeignSeriesId, report.Series);
                return null;
            }

            if (existingSeries != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Series Exists in DB.  Ensuring Series monitored", report.ForeignSeriesId, report.Series);

                if (importList.ShouldMonitorExisting && !existingSeries.Monitored)
                {
                    existingSeries.Monitored = true;
                    _seriesService.UpdateSeries(existingSeries);
                }

                return existingSeries;
            }

            if (existingImportSeries != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Series Exists in Import.", report.ForeignSeriesId, report.Series);

                return existingImportSeries;
            }

            var monitored = importList.ShouldMonitor != ImportListMonitorType.None;

            var toAdd = new Series
            {
                Metadata = new SeriesMetadata
                {
                    ForeignSeriesId = report.ForeignSeriesId,
                    Name = report.Series
                },
                Monitored = monitored,
                MonitorNewItems = importList.MonitorNewItems,
                RootFolderPath = importList.RootFolderPath,
                QualityProfileId = importList.ProfileId,
                Tags = importList.Tags,
                AddOptions = new AddSeriesOptions
                {
                    SearchForMissingIssues = importList.ShouldSearch,
                    Monitored = monitored,
                    Monitor = monitored ? MonitorTypes.All : MonitorTypes.None
                }
            };

            seriesToAdd.Add(toAdd);

            return toAdd;
        }

        public void Execute(ImportListSyncCommand message)
        {
            var processed = message.DefinitionId.HasValue ? SyncList(_importListFactory.Get(message.DefinitionId.Value)) : SyncAll();
        }
    }
}
