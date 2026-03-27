using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.IndexerSearch
{
    public interface ISearchForReleases
    {
        Task<List<DownloadDecision>> IssueSearch(int bookId, bool missingOnly, bool userInvokedSearch, bool interactiveSearch);
        Task<List<DownloadDecision>> SeriesSearch(int authorId, bool missingOnly, bool userInvokedSearch, bool interactiveSearch);
    }

    public class ReleaseSearchService : ISearchForReleases
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly IBookService _bookService;
        private readonly ISeriesService _authorService;
        private readonly IMakeDownloadDecision _makeDownloadDecision;
        private readonly Logger _logger;

        public ReleaseSearchService(IIndexerFactory indexerFactory,
                                IBookService bookService,
                                ISeriesService authorService,
                                IMakeDownloadDecision makeDownloadDecision,
                                Logger logger)
        {
            _indexerFactory = indexerFactory;
            _bookService = bookService;
            _authorService = authorService;
            _makeDownloadDecision = makeDownloadDecision;
            _logger = logger;
        }

        public async Task<List<DownloadDecision>> IssueSearch(int bookId, bool missingOnly, bool userInvokedSearch, bool interactiveSearch)
        {
            var downloadDecisions = new List<DownloadDecision>();

            var issue = _bookService.GetBook(bookId);

            var decisions = await IssueSearch(issue, missingOnly, userInvokedSearch, interactiveSearch);
            downloadDecisions.AddRange(decisions);

            return DeDupeDecisions(downloadDecisions);
        }

        public async Task<List<DownloadDecision>> SeriesSearch(int authorId, bool missingOnly, bool userInvokedSearch, bool interactiveSearch)
        {
            var downloadDecisions = new List<DownloadDecision>();

            var author = _authorService.GetSeries(authorId);

            var decisions = await SeriesSearch(author, missingOnly, userInvokedSearch, interactiveSearch);
            downloadDecisions.AddRange(decisions);

            return DeDupeDecisions(downloadDecisions);
        }

        public async Task<List<DownloadDecision>> SeriesSearch(Series author, bool missingOnly, bool userInvokedSearch, bool interactiveSearch)
        {
            var searchSpec = Get<SeriesSearchCriteria>(author, userInvokedSearch, interactiveSearch);
            var issues = _bookService.GetBooksBySeries(author.Id);

            issues = issues.Where(a => a.Monitored).ToList();

            searchSpec.Books = issues;

            return await Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
        }

        public async Task<List<DownloadDecision>> IssueSearch(Issue issue, bool missingOnly, bool userInvokedSearch, bool interactiveSearch)
        {
            var author = _authorService.GetSeries(issue.SeriesId);

            var searchSpec = Get<IssueSearchCriteria>(author, new List<Issue> { issue }, userInvokedSearch, interactiveSearch);

            searchSpec.IssueTitle = issue.Title;

            // searchSpec.IssueIsbn = issue.Isbn13;
            if (issue.ReleaseDate.HasValue)
            {
                searchSpec.IssueYear = issue.ReleaseDate.Value.Year;
            }

            return await Dispatch(indexer => indexer.Fetch(searchSpec), searchSpec);
        }

        private TSpec Get<TSpec>(Series author, List<Issue> issues, bool userInvokedSearch, bool interactiveSearch)
            where TSpec : SearchCriteriaBase, new()
        {
            var spec = new TSpec();

            spec.Books = issues;
            spec.Series = author;
            spec.UserInvokedSearch = userInvokedSearch;
            spec.InteractiveSearch = interactiveSearch;

            return spec;
        }

        private static TSpec Get<TSpec>(Series author, bool userInvokedSearch, bool interactiveSearch)
            where TSpec : SearchCriteriaBase, new()
        {
            var spec = new TSpec();
            spec.Series = author;
            spec.UserInvokedSearch = userInvokedSearch;
            spec.InteractiveSearch = interactiveSearch;

            return spec;
        }

        private async Task<List<DownloadDecision>> Dispatch(Func<IIndexer, Task<IList<ReleaseInfo>>> searchAction, SearchCriteriaBase criteriaBase)
        {
            var indexers = criteriaBase.InteractiveSearch ?
                _indexerFactory.InteractiveSearchEnabled() :
                _indexerFactory.AutomaticSearchEnabled();

            // Filter indexers to untagged indexers and indexers with intersecting tags
            indexers = indexers.Where(i => i.Definition.Tags.Empty() || i.Definition.Tags.Intersect(criteriaBase.Series.Tags).Any()).ToList();

            _logger.ProgressInfo("Searching indexers for {0}. {1} active indexers", criteriaBase, indexers.Count);

            var tasks = indexers.Select(indexer => DispatchIndexer(searchAction, indexer, criteriaBase));

            var batch = await Task.WhenAll(tasks);

            var reports = batch.SelectMany(x => x).ToList();

            _logger.ProgressDebug("Total of {0} reports were found for {1} from {2} indexers", reports.Count, criteriaBase, indexers.Count);

            // Update the last search time for all albums if at least 1 indexer was searched.
            if (indexers.Any())
            {
                var lastSearchTime = DateTime.UtcNow;
                _logger.Debug("Setting last search time to: {0}", lastSearchTime);

                criteriaBase.Books.ForEach(a => a.LastSearchTime = lastSearchTime);
                _bookService.UpdateLastSearchTime(criteriaBase.Books);
            }

            return _makeDownloadDecision.GetSearchDecision(reports, criteriaBase).ToList();
        }

        private async Task<IList<ReleaseInfo>> DispatchIndexer(Func<IIndexer, Task<IList<ReleaseInfo>>> searchAction, IIndexer indexer, SearchCriteriaBase criteriaBase)
        {
            try
            {
                return await searchAction(indexer);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while searching for {0}", criteriaBase);
            }

            return Array.Empty<ReleaseInfo>();
        }

        private List<DownloadDecision> DeDupeDecisions(List<DownloadDecision> decisions)
        {
            // De-dupe reports by guid so duplicate results aren't returned. Pick the one with the least rejections and higher indexer priority.
            return decisions.GroupBy(d => d.RemoteBook.Release.Guid)
                .Select(d => d.OrderBy(v => v.Rejections.Count()).ThenBy(v => v.RemoteBook?.Release?.IndexerPriority ?? IndexerDefinition.DefaultPriority).First())
                .ToList();
        }
    }
}
