using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.MetadataSource.Provider;

namespace NzbDrone.Core.MetadataSource.ComicVine
{
    public class ComicVineProvider
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
            // ComicVine does not expose a "changed since" endpoint via free tier.
            // Null, not empty: empty means "nothing changed" and would make a
            // delta-based refresh skip every series.
            return null;
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
                IssueNumber = IssueNumberNormalizer.Normalize(detail.IssueNumber),
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
            // Not supported — null, matching the interface contract.
            return null;
        }

        private static ProviderIssue MapIssue(Resources.ComicVineIssueSummary i)
        {
            return new ProviderIssue
            {
                ForeignIssueId = "cv:" + i.Id,
                Title = i.Name,
                IssueNumber = IssueNumberNormalizer.Normalize(i.IssueNumber),
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

            // Bare numeric ids are Metron ids — only the cv: prefix is ours.
            if (!foreignId.StartsWith("cv:"))
            {
                return null;
            }

            return int.TryParse(foreignId.Substring(3), out var id) ? id : (int?)null;
        }

        private static int? TryParseYear(string year)
        {
            return int.TryParse(year, out var y) ? y : (int?)null;
        }

        private static DateTime? TryParseDate(string date)
        {
            if (string.IsNullOrWhiteSpace(date))
            {
                return null;
            }

            // Parse as UTC: cover dates parsed as local midnight shift on the
            // UTC round-trip through the DB and then never compare equal to a
            // freshly-parsed value, making every refresh re-save every issue.
            return DateTime.TryParse(date, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var d)
                ? d
                : (DateTime?)null;
        }

        // Also used by CompositeSearchService so search results match refreshed data.
        internal static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return html;
            }

            // Very lightweight HTML strip — remove tags, then decode entities
            var stripped = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", string.Empty);
            return System.Net.WebUtility.HtmlDecode(stripped).Trim();
        }
    }
}
