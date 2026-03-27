using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Parser
{
    public interface IParsingService
    {
        Series GetSeries(string title);
        RemoteBook Map(ParsedBookInfo parsedBookInfo, SearchCriteriaBase searchCriteria = null);
        RemoteBook Map(ParsedBookInfo parsedBookInfo, int authorId, IEnumerable<int> bookIds);
        List<Issue> GetBooks(ParsedBookInfo parsedBookInfo, Series author, SearchCriteriaBase searchCriteria = null);

        ParsedBookInfo ParseBookTitleFuzzy(string title);

        // Music stuff here
        Issue GetLocalBook(string filename, Series author);

        // Comic-specific matching
        Series GetSeriesForComicRelease(ParsedComicInfo parsedComicInfo, SearchCriteriaBase searchCriteria = null);
        List<Issue> GetIssuesForComicRelease(ParsedComicInfo parsedComicInfo, Series series, SearchCriteriaBase searchCriteria = null);
        RemoteBook MapComicRelease(ParsedComicInfo parsedComicInfo, SearchCriteriaBase searchCriteria = null);
    }

    public class ParsingService : IParsingService
    {
        private readonly ISeriesService _authorService;
        private readonly IBookService _bookService;
        private readonly IEditionService _editionService;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public ParsingService(ISeriesService authorService,
                              IBookService bookService,
                              IEditionService editionService,
                              IMediaFileService mediaFileService,
                              Logger logger)
        {
            _bookService = bookService;
            _editionService = editionService;
            _authorService = authorService;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public Series GetSeries(string title)
        {
            var parsedBookInfo = Parser.ParseBookTitle(title);

            if (parsedBookInfo != null && !parsedBookInfo.SeriesName.IsNullOrWhiteSpace())
            {
                title = parsedBookInfo.SeriesName;
            }

            var authorInfo = _authorService.FindByName(title);

            if (authorInfo == null)
            {
                _logger.Debug("Trying inexact author match for {0}", title);
                authorInfo = _authorService.FindByNameInexact(title);
            }

            return authorInfo;
        }

        public RemoteBook Map(ParsedBookInfo parsedBookInfo, SearchCriteriaBase searchCriteria = null)
        {
            var remoteBook = new RemoteBook
            {
                ParsedBookInfo = parsedBookInfo,
            };

            var author = GetSeries(parsedBookInfo, searchCriteria);

            if (author == null)
            {
                return remoteBook;
            }

            remoteBook.Series = author;
            remoteBook.Books = GetBooks(parsedBookInfo, author, searchCriteria);

            return remoteBook;
        }

        public List<Issue> GetBooks(ParsedBookInfo parsedBookInfo, Series author, SearchCriteriaBase searchCriteria = null)
        {
            var bookTitle = parsedBookInfo.IssueTitle;
            var result = new List<Issue>();

            if (parsedBookInfo.IssueTitle == null)
            {
                return new List<Issue>();
            }

            Issue bookInfo = null;

            if (parsedBookInfo.Discography)
            {
                if (parsedBookInfo.DiscographyStart > 0)
                {
                    return _bookService.SeriesBooksBetweenDates(author,
                        new DateTime(parsedBookInfo.DiscographyStart, 1, 1),
                        new DateTime(parsedBookInfo.DiscographyEnd, 12, 31),
                        false);
                }

                if (parsedBookInfo.DiscographyEnd > 0)
                {
                    return _bookService.SeriesBooksBetweenDates(author,
                        new DateTime(1800, 1, 1),
                        new DateTime(parsedBookInfo.DiscographyEnd, 12, 31),
                        false);
                }

                return _bookService.GetBooksBySeries(author.Id);
            }

            if (searchCriteria != null)
            {
                var cleanTitle = Parser.CleanSeriesName(parsedBookInfo.IssueTitle);
                bookInfo = searchCriteria.Books.ExclusiveOrDefault(e => e.Title == bookTitle || e.CleanTitle == cleanTitle);
            }

            if (bookInfo == null)
            {
                // TODO: Search by Title and Year instead of just Title when matching
                bookInfo = _bookService.FindByTitle(author.SeriesMetadataId, parsedBookInfo.IssueTitle);
            }

            if (bookInfo == null)
            {
                var edition = _editionService.FindByTitle(author.SeriesMetadataId, parsedBookInfo.IssueTitle);
                bookInfo = edition?.Issue.Value;
            }

            if (bookInfo == null)
            {
                _logger.Debug("Trying inexact issue match for {0}", parsedBookInfo.IssueTitle);
                bookInfo = _bookService.FindByTitleInexact(author.SeriesMetadataId, parsedBookInfo.IssueTitle);
            }

            if (bookInfo == null)
            {
                _logger.Debug("Trying inexact edition match for {0}", parsedBookInfo.IssueTitle);
                var edition = _editionService.FindByTitleInexact(author.SeriesMetadataId, parsedBookInfo.IssueTitle);
                bookInfo = edition?.Issue.Value;
            }

            if (bookInfo != null)
            {
                result.Add(bookInfo);
            }
            else
            {
                _logger.Debug("Unable to find {0}", parsedBookInfo);
            }

            return result;
        }

        public RemoteBook Map(ParsedBookInfo parsedBookInfo, int authorId, IEnumerable<int> bookIds)
        {
            return new RemoteBook
            {
                ParsedBookInfo = parsedBookInfo,
                Series = _authorService.GetSeries(authorId),
                Books = _bookService.GetBooks(bookIds)
            };
        }

        private Series GetSeries(ParsedBookInfo parsedBookInfo, SearchCriteriaBase searchCriteria)
        {
            Series author = null;

            if (searchCriteria != null)
            {
                if (searchCriteria.Series.CleanName == parsedBookInfo.SeriesName.CleanSeriesName())
                {
                    return searchCriteria.Series;
                }
            }

            author = _authorService.FindByName(parsedBookInfo.SeriesName);

            if (author == null)
            {
                _logger.Debug("Trying inexact author match for {0}", parsedBookInfo.SeriesName);
                author = _authorService.FindByNameInexact(parsedBookInfo.SeriesName);
            }

            if (author == null)
            {
                _logger.Debug("No matching author {0}", parsedBookInfo.SeriesName);
                return null;
            }

            return author;
        }

        public ParsedBookInfo ParseBookTitleFuzzy(string title)
        {
            var bestScore = 0.0;

            Series bestSeries = null;
            Issue bestBook = null;

            var possibleSeriess = _authorService.GetReportCandidates(title);

            foreach (var author in possibleSeriess)
            {
                _logger.Trace($"Trying possible author {author}");

                var authorMatch = title.FuzzyMatch(author.Metadata.Value.Name, 0.5);
                var possibleBooks = _bookService.GetCandidates(author.SeriesMetadataId, title);

                foreach (var issue in possibleBooks)
                {
                    var bookMatch = title.FuzzyMatch(issue.Title, 0.5);
                    var score = (authorMatch.Item3 + bookMatch.Item3) / 2;

                    _logger.Trace($"Issue {issue} has score {score}");

                    if (score > bestScore)
                    {
                        bestSeries = author;
                        bestBook = issue;
                    }
                }

                var possibleEditions = _editionService.GetCandidates(author.SeriesMetadataId, title);
                foreach (var edition in possibleEditions)
                {
                    var editionMatch = title.FuzzyMatch(edition.Title, 0.5);
                    var score = (authorMatch.Item3 + editionMatch.Item3) / 2;

                    _logger.Trace($"Edition {edition} has score {score}");

                    if (score > bestScore)
                    {
                        bestSeries = author;
                        bestBook = edition.Issue.Value;
                    }
                }
            }

            _logger.Trace($"Best match: {bestSeries} {bestBook}");

            if (bestSeries != null)
            {
                return Parser.ParseBookTitleWithSearchCriteria(title, bestSeries, new List<Issue> { bestBook });
            }

            return null;
        }

        public Issue GetLocalBook(string filename, Series author)
        {
            if (Path.HasExtension(filename))
            {
                filename = Path.GetDirectoryName(filename);
            }

            var tracksInBook = _mediaFileService.GetFilesBySeries(author.Id)
                .FindAll(s => Path.GetDirectoryName(s.Path) == filename)
                .DistinctBy(s => s.IssueId)
                .ToList();

            return tracksInBook.Count == 1 ? _bookService.GetBook(tracksInBook.First().IssueId) : null;
        }

        public Series GetSeriesForComicRelease(ParsedComicInfo parsedComicInfo, SearchCriteriaBase searchCriteria = null)
        {
            if (parsedComicInfo?.SeriesTitle.IsNullOrWhiteSpace() != false)
            {
                return null;
            }

            if (searchCriteria != null)
            {
                var cleanTitle = parsedComicInfo.SeriesTitle.CleanSeriesName();
                if (searchCriteria.Series.CleanName == cleanTitle)
                {
                    return searchCriteria.Series;
                }
            }

            var series = _authorService.FindByName(parsedComicInfo.SeriesTitle);

            if (series == null)
            {
                _logger.Debug("Trying inexact series match for comic release: {0}", parsedComicInfo.SeriesTitle);
                series = _authorService.FindByNameInexact(parsedComicInfo.SeriesTitle);
            }

            return series;
        }

        public List<Issue> GetIssuesForComicRelease(ParsedComicInfo parsedComicInfo, Series series, SearchCriteriaBase searchCriteria = null)
        {
            if (parsedComicInfo == null || series == null)
            {
                return new List<Issue>();
            }

            // Match by issue number if available
            if (parsedComicInfo.IssueNumber.HasValue)
            {
                var issueNum = parsedComicInfo.IssueNumber.Value;

                if (searchCriteria?.Books != null)
                {
                    var byNumber = searchCriteria.Books.Where(b => (float)b.IssueNumber == issueNum).ToList();
                    if (byNumber.Any())
                    {
                        return byNumber;
                    }
                }

                var allIssues = _bookService.GetBooksBySeries(series.Id);
                var matched = allIssues.Where(i => (float)i.IssueNumber == issueNum).ToList();

                if (matched.Any())
                {
                    return matched;
                }
            }

            _logger.Debug("Unable to match comic issue for series {0}, issue #{1}", series.Name, parsedComicInfo.IssueNumber);
            return new List<Issue>();
        }

        public RemoteBook MapComicRelease(ParsedComicInfo parsedComicInfo, SearchCriteriaBase searchCriteria = null)
        {
            var remoteBook = new RemoteBook
            {
                ParsedBookInfo = new ParsedBookInfo
                {
                    SeriesName = parsedComicInfo?.SeriesTitle,
                    ReleaseGroup = parsedComicInfo?.ReleaseGroup,
                    Quality = parsedComicInfo?.Quality ?? new NzbDrone.Core.Qualities.QualityModel()
                }
            };

            var series = GetSeriesForComicRelease(parsedComicInfo, searchCriteria);
            if (series == null)
            {
                return remoteBook;
            }

            remoteBook.Series = series;
            remoteBook.Books = GetIssuesForComicRelease(parsedComicInfo, series, searchCriteria);

            return remoteBook;
        }
    }
}
