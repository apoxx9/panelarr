using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Books
{
    public interface IBookService
    {
        Issue GetBook(int bookId);
        List<Issue> GetBooks(IEnumerable<int> bookIds);
        List<Issue> GetBooksBySeries(int authorId);
        List<Issue> GetNextBooksBySeriesMetadataId(IEnumerable<int> authorMetadataIds);
        List<Issue> GetLastBooksBySeriesMetadataId(IEnumerable<int> authorMetadataIds);
        List<Issue> GetBooksBySeriesMetadataId(int authorMetadataId);
        List<Issue> GetBooksForRefresh(int authorMetadataId, List<string> foreignIds);
        List<Issue> GetBooksByFileIds(IEnumerable<int> fileIds);
        Issue AddBook(Issue newBook, bool doRefresh = true);
        Issue FindById(string foreignId);
        Issue FindBySlug(string titleSlug);
        Issue FindByTitle(int authorMetadataId, string title);
        Issue FindByTitleInexact(int authorMetadataId, string title);
        List<Issue> GetCandidates(int authorMetadataId, string title);
        void DeleteBook(int bookId, bool deleteFiles, bool addImportListExclusion = false);
        List<Issue> GetAllBooks();
        Issue UpdateBook(Issue issue);
        void SetBookMonitored(int bookId, bool monitored);
        void SetMonitored(IEnumerable<int> ids, bool monitored);
        void UpdateLastSearchTime(List<Issue> issues);
        PagingSpec<Issue> IssuesWithoutFiles(PagingSpec<Issue> pagingSpec);
        List<Issue> IssuesBetweenDates(DateTime start, DateTime end, bool includeUnmonitored);
        List<Issue> SeriesBooksBetweenDates(Series author, DateTime start, DateTime end, bool includeUnmonitored);
        void InsertMany(List<Issue> issues);
        void UpdateMany(List<Issue> issues);
        void DeleteMany(List<Issue> issues);
        void SetAddOptions(IEnumerable<Issue> issues);
        List<Issue> GetSeriesBooksWithFiles(Series author);
    }

    public class IssueService : IBookService,
                                IHandle<SeriesDeletedEvent>
    {
        private readonly IBookRepository _bookRepository;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public IssueService(IBookRepository bookRepository,
                           IEventAggregator eventAggregator,
                           Logger logger)
        {
            _bookRepository = bookRepository;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public Issue AddBook(Issue newBook, bool doRefresh = true)
        {
            if (newBook.SeriesMetadataId == 0)
            {
                throw new InvalidOperationException("Cannot insert issue with SeriesMetadataId = 0");
            }

            _bookRepository.Upsert(newBook);

            _eventAggregator.PublishEvent(new IssueAddedEvent(GetBook(newBook.Id), doRefresh));

            return newBook;
        }

        public void DeleteBook(int bookId, bool deleteFiles, bool addImportListExclusion = false)
        {
            var issue = _bookRepository.Get(bookId);
            issue.Series.LazyLoad();
            _bookRepository.Delete(bookId);
            _eventAggregator.PublishEvent(new IssueDeletedEvent(issue, deleteFiles, addImportListExclusion));
        }

        public Issue FindById(string foreignId)
        {
            return _bookRepository.FindById(foreignId);
        }

        public Issue FindBySlug(string titleSlug)
        {
            return _bookRepository.FindBySlug(titleSlug);
        }

        public Issue FindByTitle(int authorMetadataId, string title)
        {
            return _bookRepository.FindByTitle(authorMetadataId, title);
        }

        private List<Tuple<Func<Issue, string, double>, string>> IssueScoringFunctions(string title, string cleanTitle)
        {
            Func<Func<Issue, string, double>, string, Tuple<Func<Issue, string, double>, string>> tc = Tuple.Create;
            var scoringFunctions = new List<Tuple<Func<Issue, string, double>, string>>
            {
                tc((a, t) => a.CleanTitle.FuzzyMatch(t), cleanTitle),
                tc((a, t) => a.Title.FuzzyMatch(t), title),
                tc((a, t) => a.CleanTitle.FuzzyMatch(t), title.RemoveBracketsAndContents().CleanSeriesName()),
                tc((a, t) => a.CleanTitle.FuzzyMatch(t), title.RemoveAfterDash().CleanSeriesName()),
                tc((a, t) => a.CleanTitle.FuzzyMatch(t), title.RemoveBracketsAndContents().RemoveAfterDash().CleanSeriesName()),
                tc((a, t) => t.FuzzyContains(a.CleanTitle), cleanTitle),
                tc((a, t) => t.FuzzyContains(a.Title), title),
                tc((a, t) => a.Title.SplitBookTitle(a.SeriesMetadata.Value.Name).Item1.FuzzyMatch(t), title)
            };

            return scoringFunctions;
        }

        public Issue FindByTitleInexact(int authorMetadataId, string title)
        {
            var issues = GetBooksBySeriesMetadataId(authorMetadataId);

            foreach (var func in IssueScoringFunctions(title, title.CleanSeriesName()))
            {
                var results = FindByStringInexact(issues, func.Item1, func.Item2);
                if (results.Count == 1)
                {
                    return results[0];
                }
            }

            return null;
        }

        public List<Issue> GetCandidates(int authorMetadataId, string title)
        {
            var issues = GetBooksBySeriesMetadataId(authorMetadataId);
            var output = new List<Issue>();

            foreach (var func in IssueScoringFunctions(title, title.CleanSeriesName()))
            {
                output.AddRange(FindByStringInexact(issues, func.Item1, func.Item2));
            }

            return output.DistinctBy(x => x.Id).ToList();
        }

        private List<Issue> FindByStringInexact(List<Issue> issues, Func<Issue, string, double> scoreFunction, string title)
        {
            const double fuzzThreshold = 0.7;
            const double fuzzGap = 0.4;

            var sortedBooks = issues.Select(s => new
            {
                MatchProb = scoreFunction(s, title),
                Issue = s
            })
                .ToList()
                .OrderByDescending(s => s.MatchProb)
                .ToList();

            return sortedBooks.TakeWhile((x, i) => i == 0 || sortedBooks[i - 1].MatchProb - x.MatchProb < fuzzGap)
                .TakeWhile((x, i) => x.MatchProb > fuzzThreshold || (i > 0 && sortedBooks[i - 1].MatchProb > fuzzThreshold))
                .Select(x => x.Issue)
                .ToList();
        }

        public List<Issue> GetAllBooks()
        {
            return _bookRepository.All().ToList();
        }

        public Issue GetBook(int bookId)
        {
            return _bookRepository.Get(bookId);
        }

        public List<Issue> GetBooks(IEnumerable<int> bookIds)
        {
            return _bookRepository.Get(bookIds).ToList();
        }

        public List<Issue> GetBooksBySeries(int authorId)
        {
            return _bookRepository.GetBooks(authorId).ToList();
        }

        public List<Issue> GetNextBooksBySeriesMetadataId(IEnumerable<int> authorMetadataIds)
        {
            return _bookRepository.GetNextBooks(authorMetadataIds).ToList();
        }

        public List<Issue> GetLastBooksBySeriesMetadataId(IEnumerable<int> authorMetadataIds)
        {
            return _bookRepository.GetLastBooks(authorMetadataIds).ToList();
        }

        public List<Issue> GetBooksBySeriesMetadataId(int authorMetadataId)
        {
            return _bookRepository.GetBooksBySeriesMetadataId(authorMetadataId).ToList();
        }

        public List<Issue> GetBooksForRefresh(int authorMetadataId, List<string> foreignIds)
        {
            return _bookRepository.GetBooksForRefresh(authorMetadataId, foreignIds);
        }

        public List<Issue> GetBooksByFileIds(IEnumerable<int> fileIds)
        {
            return _bookRepository.GetBooksByFileIds(fileIds);
        }

        public void SetAddOptions(IEnumerable<Issue> issues)
        {
            _bookRepository.SetFields(issues.ToList(), s => s.AddOptions);
        }

        public PagingSpec<Issue> IssuesWithoutFiles(PagingSpec<Issue> pagingSpec)
        {
            var bookResult = _bookRepository.IssuesWithoutFiles(pagingSpec);

            return bookResult;
        }

        public List<Issue> IssuesBetweenDates(DateTime start, DateTime end, bool includeUnmonitored)
        {
            var issues = _bookRepository.IssuesBetweenDates(start.ToUniversalTime(), end.ToUniversalTime(), includeUnmonitored);

            return issues;
        }

        public List<Issue> SeriesBooksBetweenDates(Series author, DateTime start, DateTime end, bool includeUnmonitored)
        {
            var issues = _bookRepository.SeriesBooksBetweenDates(author, start.ToUniversalTime(), end.ToUniversalTime(), includeUnmonitored);

            return issues;
        }

        public List<Issue> GetSeriesBooksWithFiles(Series author)
        {
            return _bookRepository.GetSeriesBooksWithFiles(author);
        }

        public void InsertMany(List<Issue> issues)
        {
            if (issues.Any(x => x.SeriesMetadataId == 0))
            {
                throw new InvalidOperationException("Cannot insert issue with SeriesMetadataId = 0");
            }

            _bookRepository.InsertMany(issues);
        }

        public void UpdateMany(List<Issue> issues)
        {
            _bookRepository.UpdateMany(issues);
        }

        public void DeleteMany(List<Issue> issues)
        {
            _bookRepository.DeleteMany(issues);

            foreach (var issue in issues)
            {
                _eventAggregator.PublishEvent(new IssueDeletedEvent(issue, false, false));
            }
        }

        public Issue UpdateBook(Issue issue)
        {
            var storedBook = GetBook(issue.Id);
            var updatedBook = _bookRepository.Update(issue);

            _eventAggregator.PublishEvent(new IssueEditedEvent(updatedBook, storedBook));

            return updatedBook;
        }

        public void SetBookMonitored(int bookId, bool monitored)
        {
            var issue = _bookRepository.Get(bookId);
            _bookRepository.SetMonitoredFlat(issue, monitored);

            // publish issue edited event so author stats update
            _eventAggregator.PublishEvent(new IssueEditedEvent(issue, issue));

            _logger.Debug("Monitored flag for Issue:{0} was set to {1}", bookId, monitored);
        }

        public void SetMonitored(IEnumerable<int> ids, bool monitored)
        {
            _bookRepository.SetMonitored(ids, monitored);

            // publish issue edited event so author stats update
            foreach (var issue in _bookRepository.Get(ids))
            {
                _eventAggregator.PublishEvent(new IssueEditedEvent(issue, issue));
            }
        }

        public void UpdateLastSearchTime(List<Issue> issues)
        {
            _bookRepository.SetFields(issues, b => b.LastSearchTime);
        }

        public void Handle(SeriesDeletedEvent message)
        {
            var issues = GetBooksBySeriesMetadataId(message.Series.SeriesMetadataId);
            DeleteMany(issues);
        }
    }
}
