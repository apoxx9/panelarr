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
        RemoteIssue Map(ParsedIssueInfo parsedIssueInfo, int seriesId, IEnumerable<int> issueIds);
        List<Issue> GetIssues(ParsedIssueInfo parsedIssueInfo, Series series, SearchCriteriaBase searchCriteria = null);

        ParsedIssueInfo ParseIssueTitleFuzzy(string title);

        // Music stuff here
        Issue GetLocalIssue(string filename, Series series);

        // Comic-specific matching
        Series GetSeriesForComicRelease(ParsedComicInfo parsedComicInfo, SearchCriteriaBase searchCriteria = null);
        List<Issue> GetIssuesForComicRelease(ParsedComicInfo parsedComicInfo, Series series, SearchCriteriaBase searchCriteria = null);
        RemoteIssue MapComicRelease(ParsedComicInfo parsedComicInfo, SearchCriteriaBase searchCriteria = null);
    }

    public class ParsingService : IParsingService
    {
        private readonly ISeriesService _seriesService;
        private readonly IIssueService _issueService;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public ParsingService(ISeriesService seriesService,
                              IIssueService issueService,
                              IMediaFileService mediaFileService,
                              Logger logger)
        {
            _issueService = issueService;
            _seriesService = seriesService;
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

            var seriesInfo = _seriesService.FindByName(title);

            if (seriesInfo == null)
            {
                _logger.Debug("Trying inexact series match for {0}", title);
                seriesInfo = _seriesService.FindByNameInexact(title);
            }

            return seriesInfo;
        }

        public RemoteIssue Map(ParsedIssueInfo parsedIssueInfo, SearchCriteriaBase searchCriteria = null)
        {
            var remoteIssue = new RemoteIssue
            {
                ParsedIssueInfo = parsedIssueInfo,
            };

            var series = GetSeries(parsedIssueInfo, searchCriteria);

            if (series == null)
            {
                return remoteIssue;
            }

            remoteIssue.Series = series;
            remoteIssue.Issues = GetIssues(parsedIssueInfo, series, searchCriteria);

            return remoteIssue;
        }

        public List<Issue> GetIssues(ParsedIssueInfo parsedIssueInfo, Series series, SearchCriteriaBase searchCriteria = null)
        {
            var issueTitle = parsedIssueInfo.IssueTitle;
            var result = new List<Issue>();

            if (parsedIssueInfo.IssueTitle == null)
            {
                return new List<Issue>();
            }

            Issue issueMatch = null;

            if (parsedIssueInfo.Discography)
            {
                if (parsedIssueInfo.DiscographyStart > 0)
                {
                    return _issueService.SeriesIssuesBetweenDates(series,
                        new DateTime(parsedIssueInfo.DiscographyStart, 1, 1),
                        new DateTime(parsedIssueInfo.DiscographyEnd, 12, 31),
                        false);
                }

                if (parsedIssueInfo.DiscographyEnd > 0)
                {
                    return _issueService.SeriesIssuesBetweenDates(series,
                        new DateTime(1800, 1, 1),
                        new DateTime(parsedIssueInfo.DiscographyEnd, 12, 31),
                        false);
                }

                return _issueService.GetIssuesBySeries(series.Id);
            }

            // Try matching by issue number first (for comics with "#N" format titles)
            if (parsedIssueInfo.IssueTitle?.StartsWith("#") == true &&
                int.TryParse(parsedIssueInfo.IssueTitle.TrimStart('#'), out var issueNum))
            {
                if (searchCriteria != null)
                {
                    issueMatch = searchCriteria.Issues.FirstOrDefault(e => (int)e.IssueNumber == issueNum);
                }

                if (issueMatch == null)
                {
                    var seriesIssues = _issueService.GetIssuesBySeries(series.Id);
                    issueMatch = seriesIssues.FirstOrDefault(e => (int)e.IssueNumber == issueNum);
                }
            }

            if (issueMatch == null && searchCriteria != null)
            {
                var cleanTitle = Parser.CleanSeriesName(parsedIssueInfo.IssueTitle);
                issueMatch = searchCriteria.Issues.ExclusiveOrDefault(e => e.Title == issueTitle || e.CleanTitle == cleanTitle);
            }

            if (issueMatch == null)
            {
                issueMatch = _issueService.FindByTitle(series.SeriesMetadataId, parsedIssueInfo.IssueTitle);
            }

            if (issueMatch == null)
            {
                _logger.Debug("Trying inexact issue match for {0}", parsedIssueInfo.IssueTitle);
                issueMatch = _issueService.FindByTitleInexact(series.SeriesMetadataId, parsedIssueInfo.IssueTitle);
            }

            if (issueMatch != null)
            {
                result.Add(issueMatch);
            }
            else
            {
                _logger.Debug("Unable to find {0}", parsedIssueInfo);
            }

            return result;
        }

        public RemoteIssue Map(ParsedIssueInfo parsedIssueInfo, int seriesId, IEnumerable<int> issueIds)
        {
            return new RemoteIssue
            {
                ParsedIssueInfo = parsedIssueInfo,
                Series = _seriesService.GetSeries(seriesId),
                Issues = _issueService.GetIssues(issueIds)
            };
        }

        private Series GetSeries(ParsedIssueInfo parsedIssueInfo, SearchCriteriaBase searchCriteria)
        {
            Series series = null;

            if (searchCriteria != null)
            {
                if (searchCriteria.Series.CleanName == parsedIssueInfo.SeriesName.CleanSeriesName())
                {
                    return searchCriteria.Series;
                }
            }

            series = _seriesService.FindByName(parsedIssueInfo.SeriesName);

            if (series == null)
            {
                _logger.Debug("Trying inexact series match for {0}", parsedIssueInfo.SeriesName);
                series = _seriesService.FindByNameInexact(parsedIssueInfo.SeriesName);
            }

            if (series == null)
            {
                _logger.Debug("No matching series {0}", parsedIssueInfo.SeriesName);
                return null;
            }

            return series;
        }

        public ParsedIssueInfo ParseIssueTitleFuzzy(string title)
        {
            var bestScore = 0.0;

            Series bestSeries = null;
            Issue bestIssue = null;

            var possibleSeries = _seriesService.GetReportCandidates(title);

            foreach (var series in possibleSeries)
            {
                _logger.Trace($"Trying possible series {series}");

                var seriesMatch = title.FuzzyMatch(series.Metadata.Value.Name, 0.5);
                var possibleIssues = _issueService.GetCandidates(series.SeriesMetadataId, title);

                foreach (var issue in possibleIssues)
                {
                    var issueMatch = title.FuzzyMatch(issue.Title, 0.5);
                    var score = (seriesMatch.Item3 + issueMatch.Item3) / 2;

                    _logger.Trace($"Issue {issue} has score {score}");

                    if (score > bestScore)
                    {
                        bestSeries = series;
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

        public Issue GetLocalIssue(string filename, Series series)
        {
            if (Path.HasExtension(filename))
            {
                filename = Path.GetDirectoryName(filename);
            }

            var tracksInIssue = _mediaFileService.GetFilesBySeries(series.Id)
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

            var series = _seriesService.FindByName(parsedComicInfo.SeriesTitle);

            if (series == null)
            {
                _logger.Debug("Trying inexact series match for comic release: {0}", parsedComicInfo.SeriesTitle);
                series = _seriesService.FindByNameInexact(parsedComicInfo.SeriesTitle);
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
