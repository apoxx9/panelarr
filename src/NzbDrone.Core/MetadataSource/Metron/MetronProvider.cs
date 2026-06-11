using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MetadataSource.Provider;

namespace NzbDrone.Core.MetadataSource.Metron
{
    public class MetronProvider
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

            // Metron's series list responses carry no publisher; it is only
            // available from the series detail endpoint.
            return results.Select(r => new ProviderSeries
            {
                ForeignSeriesId = r.Id.ToString(),
                Name = r.Name,
                Year = r.YearBegan,
                IssueCount = r.IssueCount
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

            var issues = _client.GetIssuesBySeries(id).Select(MapListIssue).ToList();

            return new ProviderSeries
            {
                ForeignSeriesId = detail.Id.ToString(),
                Name = detail.Name,
                SortName = detail.SortName,
                Overview = detail.Description,
                Status = detail.Status,
                SeriesType = detail.SeriesType?.Name,
                Year = detail.YearBegan,
                VolumeNumber = detail.Volume,
                ForeignPublisherId = detail.Publisher?.Id.ToString(),
                PublisherName = detail.Publisher?.Name,
                Genres = detail.Genres?.Select(g => g.Name).ToList() ?? new List<string>(),

                // Metron's series detail exposes no image — use the first issue cover.
                ImageUrl = detail.Image ?? issues.FirstOrDefault(i => i.CoverUrl.IsNotNullOrWhiteSpace())?.CoverUrl,
                Issues = issues
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

            return issues.Select(MapListIssue).ToList();
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
                Title = detail.CollectionTitle.IsNotNullOrWhiteSpace()
                    ? detail.CollectionTitle
                    : detail.StoryTitles?.FirstOrDefault(),
                Overview = detail.Description,
                ReleaseDate = AsUtcDate(detail.CoverDate),
                IssueNumber = IssueNumberNormalizer.Normalize(detail.Number),
                PageCount = detail.PageCount,
                CoverUrl = detail.Image
            };
        }

        // The list endpoint's "issue" field is the composed display name
        // ("Series (Year) #Num"), not a story title — leave Title unset.
        private static ProviderIssue MapListIssue(Resources.MetronIssueListItem i)
        {
            return new ProviderIssue
            {
                ForeignIssueId = i.Id.ToString(),
                ReleaseDate = AsUtcDate(i.CoverDate),
                IssueNumber = IssueNumberNormalizer.Normalize(i.Number),
                CoverUrl = i.Image
            };
        }

        // Date-only JSON values deserialize with Kind=Unspecified and shift on the
        // UTC round-trip through the DB; pin them to UTC so refresh comparisons
        // are stable.
        private static System.DateTime? AsUtcDate(System.DateTime? date)
        {
            if (!date.HasValue)
            {
                return null;
            }

            return System.DateTime.SpecifyKind(date.Value, System.DateTimeKind.Utc);
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
    }
}
