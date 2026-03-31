using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Commands;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.IndexerSearch;
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
        private readonly IProvideBookInfo _bookInfoProxy;
        private readonly ISearchForNewBook _searchProxy;
        private readonly ISearchForNewSeries _searchSeriesProxy;
        private readonly ISeriesService _authorService;
        private readonly IIssueService _bookService;
        private readonly IAddSeriesService _addSeriesService;
        private readonly IAddIssueService _addBookService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public ImportListSyncService(IImportListFactory importListFactory,
                                     IImportListExclusionService importListExclusionService,
                                     IFetchAndParseImportList listFetcherAndParser,
                                     IProvideBookInfo bookInfoProxy,
                                     ISearchForNewBook searchProxy,
                                     ISearchForNewSeries searchSeriesProxy,
                                     ISeriesService authorService,
                                     IIssueService bookService,
                                     IAddSeriesService addSeriesService,
                                     IAddIssueService addBookService,
                                     IEventAggregator eventAggregator,
                                     IManageCommandQueue commandQueueManager,
                                     Logger logger)
        {
            _importListFactory = importListFactory;
            _importListExclusionService = importListExclusionService;
            _listFetcherAndParser = listFetcherAndParser;
            _bookInfoProxy = bookInfoProxy;
            _searchProxy = searchProxy;
            _searchSeriesProxy = searchSeriesProxy;
            _authorService = authorService;
            _bookService = bookService;
            _addSeriesService = addSeriesService;
            _addBookService = addBookService;
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
            var authorsToAdd = new List<Series>();
            var booksToAdd = new List<Issue>();

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
                        MapBookReport(report);
                    }

                    ProcessBookReport(importList, report, listExclusions, booksToAdd, authorsToAdd);
                }
                else if (report.Series.IsNotNullOrWhiteSpace() || report.ForeignSeriesId.IsNotNullOrWhiteSpace())
                {
                    if (report.ForeignSeriesId.IsNullOrWhiteSpace())
                    {
                        MapSeriesReport(report);
                    }

                    ProcessSeriesReport(importList, report, listExclusions, authorsToAdd);
                }
            }

            var addedSeries = _addSeriesService.AddSeries(authorsToAdd, false);
            var addedBooks = _addBookService.AddIssues(booksToAdd, false);

            var message = string.Format($"Import List Sync Completed. Items found: {items.Count}, Series added: {authorsToAdd.Count}, Books added: {booksToAdd.Count}");

            _logger.ProgressInfo(message);

            var toRefresh = addedSeries.Select(x => x.Id).Concat(addedBooks.Select(x => x.Series.Value.Id)).Distinct().ToList();
            if (toRefresh.Any())
            {
                _commandQueueManager.Push(new BulkRefreshSeriesCommand(toRefresh, true));
            }

            return processed;
        }

        private void MapBookReport(ImportListItemInfo report)
        {
            if (report.ForeignSeriesId.IsNotNullOrWhiteSpace() && report.ForeignIssueId.IsNotNullOrWhiteSpace())
            {
                return;
            }

            if (report.ForeignIssueId.IsNotNullOrWhiteSpace())
            {
                try
                {
                    var mappedBook = _bookInfoProxy.GetBookInfo(report.ForeignIssueId);

                    report.ForeignIssueId = mappedBook.Item2.ForeignIssueId;
                    report.Issue = mappedBook.Item2.Title;
                    report.ForeignSeriesId = mappedBook.Item3.First().ForeignSeriesId;
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
                    var mappedBook = _bookInfoProxy.GetBookInfo(report.ForeignEditionId);

                    _logger.Trace($"Mapped {report.ForeignEditionId} to [{mappedBook.Item2.ForeignIssueId}] {mappedBook.Item2.Title}");

                    report.ForeignIssueId = mappedBook.Item2.ForeignIssueId;
                    report.Issue = mappedBook.Item2.Title;
                    report.Series ??= mappedBook.Item3.First().Name;
                    report.ForeignSeriesId ??= mappedBook.Item3.First().ForeignSeriesId;
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
                var mappedBook = _searchProxy.SearchForNewBook(searchTerm, null, false).FirstOrDefault();

                if (mappedBook == null)
                {
                    _logger.Trace($"Nothing found for {report.Series} - {report.Issue}");
                    return;
                }

                _logger.Trace($"Mapped Issue {report.Issue} by Series {report.Series} to [{mappedBook.ForeignIssueId}] {mappedBook.Title}");

                report.ForeignIssueId = mappedBook.ForeignIssueId;
                report.Issue = mappedBook.Title;
                report.Series ??= mappedBook.SeriesMetadata?.Value?.Name;
                report.ForeignSeriesId ??= mappedBook.SeriesMetadata?.Value?.ForeignSeriesId;
            }
        }

        private void ProcessBookReport(ImportListDefinition importList, ImportListItemInfo report, List<ImportListExclusion> listExclusions, List<Issue> booksToAdd, List<Series> authorsToAdd)
        {
            // Check to see if issue in DB
            var existingBook = _bookService.FindById(report.ForeignIssueId);

            // Check to see if issue excluded
            var excludedBook = listExclusions.SingleOrDefault(s => s.ForeignId == report.ForeignIssueId);

            // Check to see if author excluded
            var excludedSeries = listExclusions.SingleOrDefault(s => s.ForeignId == report.ForeignSeriesId);

            if (excludedBook != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.ForeignEditionId, report.Issue);
                return;
            }

            if (excludedSeries != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion for parent author", report.ForeignEditionId, report.Issue);
                return;
            }

            if (existingBook != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Issue Exists in DB.  Ensuring Issue and Series monitored.", report.ForeignEditionId, report.Issue);

                if (importList.ShouldMonitorExisting && importList.ShouldMonitor != ImportListMonitorType.None)
                {
                    if (!existingBook.Monitored)
                    {
                        _bookService.SetIssueMonitored(existingBook.Id, true);

                        if (importList.ShouldMonitor == ImportListMonitorType.SpecificBook)
                        {
                            _commandQueueManager.Push(new IssueSearchCommand(new List<int> { existingBook.Id }));
                        }
                    }

                    var existingSeries = existingBook.Series.Value;
                    var doSearch = false;

                    if (importList.ShouldMonitor == ImportListMonitorType.EntireSeries)
                    {
                        if (existingSeries.Books.Value.Any(x => !x.Monitored))
                        {
                            doSearch = true;
                            _bookService.SetMonitored(existingSeries.Books.Value.Select(x => x.Id), true);
                        }
                    }

                    if (!existingSeries.Monitored)
                    {
                        doSearch = true;
                        existingSeries.Monitored = true;
                        _authorService.UpdateSeries(existingSeries);
                    }

                    if (doSearch)
                    {
                        _commandQueueManager.Push(new MissingIssueSearchCommand(existingSeries.Id));
                    }
                }

                return;
            }

            // Append Issue if not already in DB or already on add list
            if (booksToAdd.All(s => s.ForeignIssueId != report.ForeignIssueId))
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
                        SearchForMissingBooks = importList.ShouldSearch,
                        Monitored = monitored,
                        Monitor = monitored ? MonitorTypes.All : MonitorTypes.None
                    }
                };

                if (report.ForeignSeriesId != null && report.Series != null)
                {
                    toAddSeries = ProcessSeriesReport(importList, report, listExclusions, authorsToAdd);
                }

                var toAdd = new Issue
                {
                    ForeignIssueId = report.ForeignIssueId,
                    Monitored = monitored,
                    Series = toAddSeries,
                    AddOptions = new AddIssueOptions
                    {
                        // Only search for new issue for existing authors
                        // New author searches are triggered by SearchForMissingBooks
                        SearchForNewBook = importList.ShouldSearch && toAddSeries.Id > 0
                    }
                };

                if (importList.ShouldMonitor == ImportListMonitorType.SpecificBook && toAddSeries.AddOptions != null)
                {
                    Debug.Assert(toAddSeries.Id == 0, "new author added but ID is not 0");
                    toAddSeries.AddOptions.IssuesToMonitor.Add(toAdd.ForeignIssueId);
                }

                booksToAdd.Add(toAdd);
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

        private Series ProcessSeriesReport(ImportListDefinition importList, ImportListItemInfo report, List<ImportListExclusion> listExclusions, List<Series> authorsToAdd)
        {
            if (report.ForeignSeriesId == null)
            {
                return null;
            }

            // Check to see if author in DB
            var existingSeries = _authorService.FindById(report.ForeignSeriesId);

            // Check to see if author excluded
            var excludedSeries = listExclusions.SingleOrDefault(s => s.ForeignId == report.ForeignSeriesId);

            // Check to see if author in import
            var existingImportSeries = authorsToAdd.Find(i => i.ForeignSeriesId == report.ForeignSeriesId);

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
                    _authorService.UpdateSeries(existingSeries);
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
                    SearchForMissingBooks = importList.ShouldSearch,
                    Monitored = monitored,
                    Monitor = monitored ? MonitorTypes.All : MonitorTypes.None
                }
            };

            authorsToAdd.Add(toAdd);

            return toAdd;
        }

        public void Execute(ImportListSyncCommand message)
        {
            var processed = message.DefinitionId.HasValue ? SyncList(_importListFactory.Get(message.DefinitionId.Value)) : SyncAll();

            _eventAggregator.PublishEvent(new ImportListSyncCompleteEvent(processed));
        }
    }
}
