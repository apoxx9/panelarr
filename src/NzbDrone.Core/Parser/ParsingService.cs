using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Parser
{
    public interface IParsingService
    {
        Series GetSeries(string title);
        RemoteIssue Map(ParsedIssueInfo parsedIssueInfo, SearchCriteriaBase searchCriteria = null);
        RemoteIssue Map(ParsedIssueInfo parsedIssueInfo, int authorId, IEnumerable<int> bookIds);
        List<Issue> GetIssues(ParsedIssueInfo parsedIssueInfo, Series author, SearchCriteriaBase searchCriteria = null);

        ParsedIssueInfo ParseIssueTitleFuzzy(string title);

        // Music stuff here
        Issue GetLocalIssue(string filename, Series author);

        // Comic-specific matching
        Series GetSeriesForComicRelease(ParsedComicInfo parsedComicInfo, SearchCriteriaBase searchCriteria = null);
        List<Issue> GetIssuesForComicRelease(ParsedComicInfo parsedComicInfo, Series series, SearchCriteriaBase searchCriteria = null);
        RemoteIssue MapComicRelease(ParsedComicInfo parsedComicInfo, SearchCriteriaBase searchCriteria = null);
    }

    public class ParsingService : IParsingService
    {
        private readonly ISeriesService _authorService;
        private readonly IIssueService _issueService;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public ParsingService(ISeriesService authorService,
                              IIssueService bookService,
                              IMediaFileService mediaFileService,
                              Logger logger)
        {
            _issueService = bookService;
            _authorService = authorService;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public Series GetSeries(string title)
        {
            var parsedIssueInfo = Parser.ParseIssueTitle(title);

            if (parsedIssueInfo != null && !parsedIssueInfo.SeriesName.IsNullOrWhiteSpace())
            {
                title = parsedIssueInfo.SeriesName;
            }

            var authorInfo = _authorService.FindByName(title);

            if (authorInfo == null)
            {
                _logger.Debug("Trying inexact series match for {0}", title);
                authorInfo = _authorService.FindByNameInexact(title);
            }

            return authorInfo;
        }

        public RemoteIssue Map(ParsedIssueInfo parsedIssueInfo, SearchCriteriaBase searchCriteria = null)
        {
            var remoteIssue = new RemoteIssue
            {
                ParsedIssueInfo = parsedIssueInfo,
            };

            var author = GetSeries(parsedIssueInfo, searchCriteria);

            if (author == null)
            {
                return remoteIssue;
            }

            remoteIssue.Series = author;
            remoteIssue.Issues = GetIssues(parsedIssueInfo, author, searchCriteria);

            return remoteIssue;
        }

        public List<Issue> GetIssues(ParsedIssueInfo parsedIssueInfo, Series author, SearchCriteriaBase searchCriteria = null)
        {
            var bookTitle = parsedIssueInfo.IssueTitle;
            var result = new List<Issue>();

            if (parsedIssueInfo.IssueTitle == null)
            {
                return new List<Issue>();
            }

            Issue bookInfo = null;

            if (parsedIssueInfo.Discography)
            {
                if (parsedIssueInfo.DiscographyStart > 0)
                {
                    return _issueService.SeriesIssuesBetweenDates(author,
                        new DateTime(parsedIssueInfo.DiscographyStart, 1, 1),
                        new DateTime(parsedIssueInfo.DiscographyEnd, 12, 31),
                        false);
                }

                if (parsedIssueInfo.DiscographyEnd > 0)
                {
                    return _issueService.SeriesIssuesBetweenDates(author,
                        new DateTime(1800, 1, 1),
                        new DateTime(parsedIssueInfo.DiscographyEnd, 12, 31),
                        false);
                }

                return _issueService.GetIssuesBySeries(author.Id);
            }

            // Try matching by issue number first (for comics with "#N" format titles)
            if (parsedIssueInfo.IssueTitle?.StartsWith("#") == true &&
                int.TryParse(parsedIssueInfo.IssueTitle.TrimStart('#'), out var issueNum))
            {
                if (searchCriteria != null)
                {
                    bookInfo = searchCriteria.Issues.FirstOrDefault(e => (int)e.IssueNumber == issueNum);
                }

                if (bookInfo == null)
                {
                    var seriesIssues = _issueService.GetIssuesBySeries(author.Id);
                    bookInfo = seriesIssues.FirstOrDefault(e => (int)e.IssueNumber == issueNum);
                }
            }

            if (bookInfo == null && searchCriteria != null)
            {
                var cleanTitle = Parser.CleanSeriesName(parsedIssueInfo.IssueTitle);
                bookInfo = searchCriteria.Issues.ExclusiveOrDefault(e => e.Title == bookTitle || e.CleanTitle == cleanTitle);
            }

            if (bookInfo == null)
            {
                bookInfo = _issueService.FindByTitle(author.SeriesMetadataId, parsedIssueInfo.IssueTitle);
            }

            if (bookInfo == null)
            {
                _logger.Debug("Trying inexact issue match for {0}", parsedIssueInfo.IssueTitle);
                bookInfo = _issueService.FindByTitleInexact(author.SeriesMetadataId, parsedIssueInfo.IssueTitle);
            }

            if (bookInfo != null)
            {
                result.Add(bookInfo);
            }
            else
            {
                _logger.Debug("Unable to find {0}", parsedIssueInfo);
            }

            return result;
        }

        public RemoteIssue Map(ParsedIssueInfo parsedIssueInfo, int authorId, IEnumerable<int> bookIds)
        {
            return new RemoteIssue
            {
                ParsedIssueInfo = parsedIssueInfo,
                Series = _authorService.GetSeries(authorId),
                Issues = _issueService.GetIssues(bookIds)
            };
        }

        private Series GetSeries(ParsedIssueInfo parsedIssueInfo, SearchCriteriaBase searchCriteria)
        {
            Series author = null;

            if (searchCriteria != null)
            {
                if (searchCriteria.Series.CleanName == parsedIssueInfo.SeriesName.CleanSeriesName())
                {
                    return searchCriteria.Series;
                }
            }

            author = _authorService.FindByName(parsedIssueInfo.SeriesName);

            if (author == null)
            {
                _logger.Debug("Trying inexact series match for {0}", parsedIssueInfo.SeriesName);
                author = _authorService.FindByNameInexact(parsedIssueInfo.SeriesName);
            }

            if (author == null)
            {
                _logger.Debug("No matching series {0}", parsedIssueInfo.SeriesName);
                return null;
            }

            return author;
        }

        public ParsedIssueInfo ParseIssueTitleFuzzy(string title)
        {
            var bestScore = 0.0;

            Series bestSeries = null;
            Issue bestIssue = null;

            var possibleSeries = _authorService.GetReportCandidates(title);

            foreach (var author in possibleSeries)
            {
                _logger.Trace($"Trying possible series {author}");

                var authorMatch = title.FuzzyMatch(author.Metadata.Value.Name, 0.5);
                var possibleIssues = _issueService.GetCandidates(author.SeriesMetadataId, title);

                foreach (var issue in possibleIssues)
                {
                    var bookMatch = title.FuzzyMatch(issue.Title, 0.5);
                    var score = (authorMatch.Item3 + bookMatch.Item3) / 2;

                    _logger.Trace($"Issue {issue} has score {score}");

                    if (score > bestScore)
                    {
                        bestSeries = author;
                        bestIssue = issue;
                    }
                }
            }

            _logger.Trace($"Best match: {bestSeries} {bestIssue}");

            if (bestSeries != null)
            {
                return Parser.ParseIssueTitleWithSearchCriteria(title, bestSeries, new List<Issue> { bestIssue });
            }

            return null;
        }

        public Issue GetLocalIssue(string filename, Series author)
        {
            if (Path.HasExtension(filename))
            {
                filename = Path.GetDirectoryName(filename);
            }

            var tracksInIssue = _mediaFileService.GetFilesBySeries(author.Id)
                .FindAll(s => Path.GetDirectoryName(s.Path) == filename)
                .DistinctBy(s => s.IssueId)
                .ToList();

            return tracksInIssue.Count == 1 ? _issueService.GetIssue(tracksInIssue.First().IssueId) : null;
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

                if (searchCriteria?.Issues != null)
                {
                    var byNumber = searchCriteria.Issues.Where(b => (float)b.IssueNumber == issueNum).ToList();
                    if (byNumber.Any())
                    {
                        return byNumber;
                    }
                }

                var allIssues = _issueService.GetIssuesBySeries(series.Id);
                var matched = allIssues.Where(i => (float)i.IssueNumber == issueNum).ToList();

                if (matched.Any())
                {
                    return matched;
                }
            }

            _logger.Debug("Unable to match comic issue for series {0}, issue #{1}", series.Name, parsedComicInfo.IssueNumber);
            return new List<Issue>();
        }

        public RemoteIssue MapComicRelease(ParsedComicInfo parsedComicInfo, SearchCriteriaBase searchCriteria = null)
        {
            var remoteIssue = new RemoteIssue
            {
                ParsedIssueInfo = new ParsedIssueInfo
                {
                    SeriesName = parsedComicInfo?.SeriesTitle,
                    ReleaseGroup = parsedComicInfo?.ReleaseGroup,
                    Quality = parsedComicInfo?.Quality ?? new NzbDrone.Core.Qualities.QualityModel()
                }
            };

            var series = GetSeriesForComicRelease(parsedComicInfo, searchCriteria);
            if (series == null)
            {
                return remoteIssue;
            }

            remoteIssue.Series = series;
            remoteIssue.Issues = GetIssuesForComicRelease(parsedComicInfo, series, searchCriteria);

            return remoteIssue;
        }
    }
}
