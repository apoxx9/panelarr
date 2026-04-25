using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.MetadataSource.Provider;

namespace NzbDrone.Core.MetadataSource.ComicVine
{
    public class ComicVineProvider : IMetadataProvider
    {
        private readonly IComicVineApiClient _client;
        private readonly Logger _logger;

        public ComicVineProvider(IComicVineApiClient client, Logger logger)
        {
            _client = client;
            _logger = logger;
        }

        public List<ProviderSeries> SearchSeries(string title)
        {
            _logger.Debug("Searching ComicVine for series: {0}", title);

            var results = _client.SearchSeries(title);

            return results.Select(v => new ProviderSeries
            {
                ForeignSeriesId = "cv:" + v.Id,
                Name = v.Name,
                Year = TryParseYear(v.StartYear),
                ForeignPublisherId = v.Publisher != null ? "cv:" + v.Publisher.Id : null
            }).ToList();
        }

        public ProviderSeries GetSeriesInfo(string foreignSeriesId)
        {
            var id = ParseCvId(foreignSeriesId);
            if (id == null)
            {
                return null;
            }

            var detail = _client.GetVolume(id.Value);
            if (detail == null)
            {
                return null;
            }

            // Volume detail issues only contain id/name/issue_number — always fetch
            // full issue list from the issues endpoint to get cover_date and image
            var issues = _client.GetIssues(id.Value);

            return new ProviderSeries
            {
                ForeignSeriesId = "cv:" + detail.Id,
                Name = detail.Name,
                Overview = StripHtml(detail.Description),
                Year = TryParseYear(detail.StartYear),
                ForeignPublisherId = detail.Publisher != null ? "cv:" + detail.Publisher.Id : null,
                PublisherName = detail.Publisher?.Name,
                ImageUrl = detail.Image?.OriginalUrl ?? detail.Image?.MediumUrl,
                IssueCount = detail.CountOfIssues > 0 ? detail.CountOfIssues : issues.Count,
                Issues = issues.Select(MapIssue).ToList()
            };
        }

        public List<string> GetChangedSeries(long epochSeconds)
        {
            // ComicVine does not expose a "changed since" endpoint via free tier
            return new List<string>();
        }

        public List<ProviderIssue> GetIssues(string foreignSeriesId)
        {
            var id = ParseCvId(foreignSeriesId);
            if (id == null)
            {
                return new List<ProviderIssue>();
            }

            return _client.GetIssues(id.Value).Select(MapIssue).ToList();
        }

        public ProviderIssue GetIssueInfo(string foreignIssueId)
        {
            var id = ParseCvId(foreignIssueId);
            if (id == null)
            {
                return null;
            }

            var detail = _client.GetIssue(id.Value);
            if (detail == null)
            {
                return null;
            }

            return new ProviderIssue
            {
                ForeignIssueId = "cv:" + detail.Id,
                Title = detail.Name,
                Overview = StripHtml(detail.Description),
                IssueNumber = TryParseIssueNumber(detail.IssueNumber),
                ReleaseDate = TryParseDate(detail.CoverDate),
                CoverUrl = detail.Image?.MediumUrl
            };
        }

        public ProviderPublisher GetPublisher(string foreignPublisherId)
        {
            var id = ParseCvId(foreignPublisherId);
            if (id == null)
            {
                return null;
            }

            var detail = _client.GetPublisher(id.Value);
            if (detail == null)
            {
                return null;
            }

            return new ProviderPublisher
            {
                ForeignPublisherId = "cv:" + detail.Id,
                Name = detail.Name,
                Description = StripHtml(detail.Description),
                ImageUrl = detail.Image?.MediumUrl
            };
        }

        public List<string> GetNewReleases(long epochSeconds)
        {
            return new List<string>();
        }

        private static ProviderIssue MapIssue(Resources.ComicVineIssueSummary i)
        {
            return new ProviderIssue
            {
                ForeignIssueId = "cv:" + i.Id,
                Title = i.Name,
                IssueNumber = TryParseIssueNumber(i.IssueNumber),
                ReleaseDate = TryParseDate(i.CoverDate),
                CoverUrl = i.Image?.OriginalUrl ?? i.Image?.MediumUrl
            };
        }

        private static int? ParseCvId(string foreignId)
        {
            if (string.IsNullOrWhiteSpace(foreignId))
            {
                return null;
            }

            var raw = foreignId.StartsWith("cv:") ? foreignId.Substring(3) : foreignId;
            return int.TryParse(raw, out var id) ? id : (int?)null;
        }

        private static int? TryParseYear(string year)
        {
            return int.TryParse(year, out var y) ? y : (int?)null;
        }

        private static int? TryParseIssueNumber(string number)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                return null;
            }

            return int.TryParse(number, out var n) ? n : (int?)null;
        }

        private static DateTime? TryParseDate(string date)
        {
            if (string.IsNullOrWhiteSpace(date))
            {
                return null;
            }

            return DateTime.TryParse(date, out var d) ? d : (DateTime?)null;
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return html;
            }

            // Very lightweight HTML strip — just remove tags
            return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", string.Empty).Trim();
        }
    }
}
