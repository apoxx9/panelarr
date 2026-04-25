using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.MetadataSource.Provider;

namespace NzbDrone.Core.MetadataSource.Metron
{
    public class MetronProvider : IMetadataProvider
    {
        private readonly IMetronApiClient _client;
        private readonly Logger _logger;

        public MetronProvider(IMetronApiClient client, Logger logger)
        {
            _client = client;
            _logger = logger;
        }

        public List<ProviderSeries> SearchSeries(string title)
        {
            _logger.Debug("Searching Metron for series: {0}", title);

            var results = _client.SearchSeries(title);

            return results.Select(r => new ProviderSeries
            {
                ForeignSeriesId = r.Id.ToString(),
                Name = r.Name,
                Year = r.YearBegan,
                ForeignPublisherId = r.Publisher?.Id.ToString()
            }).ToList();
        }

        public ProviderSeries GetSeriesInfo(string foreignSeriesId)
        {
            if (!int.TryParse(foreignSeriesId, out var id))
            {
                return null;
            }

            var detail = _client.GetSeriesDetail(id);
            if (detail == null)
            {
                return null;
            }

            var issues = _client.GetIssuesBySeries(id);

            return new ProviderSeries
            {
                ForeignSeriesId = detail.Id.ToString(),
                Name = detail.Name,
                SortName = detail.SortName,
                Overview = detail.Description,
                Status = detail.Status,
                SeriesType = detail.SeriesType?.Name,
                Year = detail.YearBegan,
                ForeignPublisherId = detail.Publisher?.Id.ToString(),
                Genres = detail.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),
                ImageUrl = detail.Image,
                Issues = issues.Select(i => new ProviderIssue
                {
                    ForeignIssueId = i.Id.ToString(),
                    Title = i.IssueName,
                    ReleaseDate = i.CoverDate,
                    IssueNumber = ParseIssueNumber(i.Number)
                }).ToList()
            };
        }

        public List<string> GetChangedSeries(long epochSeconds)
        {
            // Metron does not currently expose a "changed since" endpoint.
            // Return empty; full refresh is used instead.
            return new List<string>();
        }

        public List<string> GetNewReleases(long epochSeconds)
        {
            // Metron does not currently expose a "new releases since" endpoint.
            return new List<string>();
        }

        public List<ProviderIssue> GetIssues(string foreignSeriesId)
        {
            if (!int.TryParse(foreignSeriesId, out var id))
            {
                return new List<ProviderIssue>();
            }

            var issues = _client.GetIssuesBySeries(id);

            return issues.Select(i => new ProviderIssue
            {
                ForeignIssueId = i.Id.ToString(),
                Title = i.IssueName,
                ReleaseDate = i.CoverDate,
                IssueNumber = ParseIssueNumber(i.Number)
            }).ToList();
        }

        public ProviderIssue GetIssueInfo(string foreignIssueId)
        {
            if (!int.TryParse(foreignIssueId, out var id))
            {
                return null;
            }

            var detail = _client.GetIssueDetail(id);
            if (detail == null)
            {
                return null;
            }

            return new ProviderIssue
            {
                ForeignIssueId = detail.Id.ToString(),
                Title = detail.IssueName,
                Overview = detail.Description,
                ReleaseDate = detail.CoverDate,
                IssueNumber = ParseIssueNumber(detail.Number),
                PageCount = detail.PageCount,
                CoverUrl = detail.Image
            };
        }

        public ProviderPublisher GetPublisher(string foreignPublisherId)
        {
            if (!int.TryParse(foreignPublisherId, out var id))
            {
                return null;
            }

            var detail = _client.GetPublisherDetail(id);
            if (detail == null)
            {
                return null;
            }

            return new ProviderPublisher
            {
                ForeignPublisherId = detail.Id.ToString(),
                Name = detail.Name,
                Description = detail.Description,
                ImageUrl = detail.Image
            };
        }

        private static int? ParseIssueNumber(string number)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                return null;
            }

            return int.TryParse(number, out var n) ? n : (int?)null;
        }
    }
}
