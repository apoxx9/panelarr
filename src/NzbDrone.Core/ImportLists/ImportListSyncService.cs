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
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists
{
    public class ImportListSyncService : IExecute<ImportListSyncCommand>
    {
        private readonly IImportListFactory _importListFactory;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly IFetchAndParseImportList _listFetcherAndParser;
        private readonly IGoodreadsProxy _goodreadsProxy;
        private readonly IGoodreadsSearchProxy _goodreadsSearchProxy;
        private readonly IProvideBookInfo _bookInfoProxy;
        private readonly ISeriesService _authorService;
        private readonly IBookService _bookService;
        private readonly IAddSeriesService _addSeriesService;
        private readonly IAddBookService _addBookService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public ImportListSyncService(IImportListFactory importListFactory,
                                     IImportListExclusionService importListExclusionService,
                                     IFetchAndParseImportList listFetcherAndParser,
                                     IGoodreadsProxy goodreadsProxy,
                                     IGoodreadsSearchProxy goodreadsSearchProxy,
                                     IProvideBookInfo bookInfoProxy,
                                     ISeriesService authorService,
                                     IBookService bookService,
                                     IAddSeriesService addSeriesService,
                                     IAddBookService addBookService,
                                     IEventAggregator eventAggregator,
                                     IManageCommandQueue commandQueueManager,
                                     Logger logger)
        {
            _importListFactory = importListFactory;
            _importListExclusionService = importListExclusionService;
            _listFetcherAndParser = listFetcherAndParser;
            _goodreadsProxy = goodreadsProxy;
            _goodreadsSearchProxy = goodreadsSearchProxy;
            _bookInfoProxy = bookInfoProxy;
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

                if (report.Issue.IsNotNullOrWhiteSpace() || report.EditionGoodreadsId.IsNotNullOrWhiteSpace())
                {
                    if (report.EditionGoodreadsId.IsNullOrWhiteSpace() || report.SeriesGoodreadsId.IsNullOrWhiteSpace() || report.IssueGoodreadsId.IsNullOrWhiteSpace())
                    {
                        MapBookReport(report);
                    }

                    ProcessBookReport(importList, report, listExclusions, booksToAdd, authorsToAdd);
                }
                else if (report.Series.IsNotNullOrWhiteSpace() || report.SeriesGoodreadsId.IsNotNullOrWhiteSpace())
                {
                    if (report.SeriesGoodreadsId.IsNullOrWhiteSpace())
                    {
                        MapSeriesReport(report);
                    }

                    ProcessSeriesReport(importList, report, listExclusions, authorsToAdd);
                }
            }

            var addedSeries = _addSeriesService.AddSeries(authorsToAdd, false);
            var addedBooks = _addBookService.AddBooks(booksToAdd, false);

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
            if (report.SeriesGoodreadsId.IsNotNullOrWhiteSpace() && report.IssueGoodreadsId.IsNotNullOrWhiteSpace())
            {
                return;
            }

            if (report.EditionGoodreadsId.IsNotNullOrWhiteSpace() && int.TryParse(report.EditionGoodreadsId, out var goodreadsId))
            {
                try
                {
                    var remoteBook = _goodreadsProxy.GetBookInfo(report.EditionGoodreadsId);

                    _logger.Trace($"Mapped {report.EditionGoodreadsId} to [{remoteBook.ForeignIssueId}] {remoteBook.Title}");

                    report.IssueGoodreadsId = remoteBook.ForeignIssueId;
                    report.Issue = remoteBook.Title;
                    report.Series ??= remoteBook.SeriesMetadata.Value.Name;
                    report.SeriesGoodreadsId ??= remoteBook.SeriesMetadata.Value.ForeignSeriesId;
                }
                catch (IssueNotFoundException)
                {
                    _logger.Debug($"Nothing found for edition [{report.EditionGoodreadsId}]");
                    report.EditionGoodreadsId = null;
                }
            }
            else if (report.IssueGoodreadsId.IsNotNullOrWhiteSpace())
            {
                var mappedBook = _bookInfoProxy.GetBookInfo(report.IssueGoodreadsId);

                report.IssueGoodreadsId = mappedBook.Item2.ForeignIssueId;
                report.Issue = mappedBook.Item2.Title;
                report.SeriesGoodreadsId = mappedBook.Item3.First().ForeignSeriesId;
            }
            else
            {
                var mappedBook = _goodreadsSearchProxy.Search($"{report.Issue} {report.Series}").FirstOrDefault();

                if (mappedBook == null)
                {
                    _logger.Trace($"Nothing found for {report.Series} - {report.Issue}");
                    return;
                }

                _logger.Trace($"Mapped Issue {report.Issue} by Series {report.Series} to [{mappedBook.WorkId}] {mappedBook.IssueTitleBare}");

                report.IssueGoodreadsId = mappedBook.WorkId.ToString();
                report.Issue = mappedBook.IssueTitleBare;
                report.Series ??= mappedBook.Series.Name;
                report.SeriesGoodreadsId ??= mappedBook.Series.Id.ToString();
                report.EditionGoodreadsId = mappedBook.IssueId.ToString();
            }
        }

        private void ProcessBookReport(ImportListDefinition importList, ImportListItemInfo report, List<ImportListExclusion> listExclusions, List<Issue> booksToAdd, List<Series> authorsToAdd)
        {
            // Check to see if issue in DB
            var existingBook = _bookService.FindById(report.IssueGoodreadsId);

            // Check to see if issue excluded
            var excludedBook = listExclusions.SingleOrDefault(s => s.ForeignId == report.IssueGoodreadsId);

            // Check to see if author excluded
            var excludedSeries = listExclusions.SingleOrDefault(s => s.ForeignId == report.SeriesGoodreadsId);

            if (excludedBook != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.EditionGoodreadsId, report.Issue);
                return;
            }

            if (excludedSeries != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion for parent author", report.EditionGoodreadsId, report.Issue);
                return;
            }

            if (existingBook != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Issue Exists in DB.  Ensuring Issue and Series monitored.", report.EditionGoodreadsId, report.Issue);

                if (importList.ShouldMonitorExisting && importList.ShouldMonitor != ImportListMonitorType.None)
                {
                    if (!existingBook.Monitored)
                    {
                        _bookService.SetBookMonitored(existingBook.Id, true);

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
            if (booksToAdd.All(s => s.ForeignIssueId != report.IssueGoodreadsId))
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

                if (report.SeriesGoodreadsId != null && report.Series != null)
                {
                    toAddSeries = ProcessSeriesReport(importList, report, listExclusions, authorsToAdd);
                }

                var toAdd = new Issue
                {
                    ForeignIssueId = report.IssueGoodreadsId,
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
            var mappedBook = _goodreadsSearchProxy.Search(report.Series).FirstOrDefault();

            if (mappedBook == null)
            {
                _logger.Trace($"Nothing found for {report.Series}");
                return;
            }

            _logger.Trace($"Mapped {report.Series} to [{mappedBook.Series.Name}]");

            report.Series = mappedBook.Series.Name;
            report.SeriesGoodreadsId = mappedBook.Series.Id.ToString();
        }

        private Series ProcessSeriesReport(ImportListDefinition importList, ImportListItemInfo report, List<ImportListExclusion> listExclusions, List<Series> authorsToAdd)
        {
            if (report.SeriesGoodreadsId == null)
            {
                return null;
            }

            // Check to see if author in DB
            var existingSeries = _authorService.FindById(report.SeriesGoodreadsId);

            // Check to see if author excluded
            var excludedSeries = listExclusions.SingleOrDefault(s => s.ForeignId == report.SeriesGoodreadsId);

            // Check to see if author in import
            var existingImportSeries = authorsToAdd.Find(i => i.ForeignSeriesId == report.SeriesGoodreadsId);

            if (excludedSeries != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.SeriesGoodreadsId, report.Series);
                return null;
            }

            if (existingSeries != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Series Exists in DB.  Ensuring Series monitored", report.SeriesGoodreadsId, report.Series);

                if (importList.ShouldMonitorExisting && !existingSeries.Monitored)
                {
                    existingSeries.Monitored = true;
                    _authorService.UpdateSeries(existingSeries);
                }

                return existingSeries;
            }

            if (existingImportSeries != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Series Exists in Import.", report.SeriesGoodreadsId, report.Series);

                return existingImportSeries;
            }

            var monitored = importList.ShouldMonitor != ImportListMonitorType.None;

            var toAdd = new Series
            {
                Metadata = new SeriesMetadata
                {
                    ForeignSeriesId = report.SeriesGoodreadsId,
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
