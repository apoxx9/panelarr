using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.Provider;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.MetadataSource.Metron
{
    public interface IMetronMapper
    {
        (SeriesMetadata Metadata, Series Series) MapSeries(ProviderSeries providerSeries);
        Issue MapIssue(ProviderIssue providerIssue, int seriesMetadataId);
        Publisher MapPublisher(ProviderPublisher providerPublisher);
        void EnrichWithDbIds(Series series, SeriesMetadata metadata, List<Series> existingSeries);
    }

    public class MetronMapper : IMetronMapper
    {
        public (SeriesMetadata Metadata, Series Series) MapSeries(ProviderSeries providerSeries)
        {
            if (providerSeries == null)
            {
                return (null, null);
            }

            var metadata = new SeriesMetadata
            {
                ForeignSeriesId = providerSeries.ForeignSeriesId,
                TitleSlug = providerSeries.ForeignSeriesId?.Replace(":", "-"),
                Name = providerSeries.Name,
                SortName = providerSeries.SortName ?? providerSeries.Name,
                Overview = providerSeries.Overview,
                Disambiguation = providerSeries.PublisherName,
                Status = MapSeriesStatus(providerSeries.Status),
                SeriesType = MapSeriesType(providerSeries.SeriesType),
                Year = providerSeries.Year,
                VolumeNumber = providerSeries.IssueCount,
                Genres = providerSeries.Genres ?? new List<string>()
            };

            if (providerSeries.ImageUrl.IsNotNullOrWhiteSpace())
            {
                metadata.Images = new List<NzbDrone.Core.MediaCover.MediaCover>
                {
                    new NzbDrone.Core.MediaCover.MediaCover
                    {
                        CoverType = NzbDrone.Core.MediaCover.MediaCoverTypes.Poster,
                        Url = providerSeries.ImageUrl,
                        RemoteUrl = providerSeries.ImageUrl
                    }
                };
            }

            var series = new Series
            {
                Metadata = metadata,
                CleanName = metadata.Name.CleanSeriesName(),
                Monitored = false
            };

            return (metadata, series);
        }

        public Issue MapIssue(ProviderIssue providerIssue, int seriesMetadataId)
        {
            if (providerIssue == null)
            {
                return null;
            }

            return new Issue
            {
                ForeignIssueId = providerIssue.ForeignIssueId,
                TitleSlug = providerIssue.ForeignIssueId?.Replace(":", "-"),
                Title = providerIssue.Title ?? string.Empty,
                ReleaseDate = providerIssue.ReleaseDate,
                SeriesMetadataId = seriesMetadataId,
                IssueNumber = providerIssue.IssueNumber.HasValue ? (float)providerIssue.IssueNumber.Value : 0f,
                IssueType = MapIssueType(providerIssue.IssueType),
                PageCount = providerIssue.PageCount ?? 0,
                CoverArtUrl = providerIssue.CoverUrl ?? string.Empty,
                Overview = providerIssue.Overview ?? string.Empty,
                CleanTitle = (providerIssue.Title ?? string.Empty).CleanSeriesName(),
                Monitored = false
            };
        }

        public Publisher MapPublisher(ProviderPublisher providerPublisher)
        {
            if (providerPublisher == null)
            {
                return null;
            }

            return new Publisher
            {
                ForeignPublisherId = providerPublisher.ForeignPublisherId,
                Name = providerPublisher.Name,
                CleanName = providerPublisher.Name.CleanSeriesName(),
                Description = providerPublisher.Description ?? string.Empty
            };
        }

        public void EnrichWithDbIds(Series series, SeriesMetadata metadata, List<Series> existingSeries)
        {
            var existing = existingSeries.SingleOrDefault(
                s => s.ForeignSeriesId == metadata.ForeignSeriesId);

            if (existing != null)
            {
                series.UseDbFieldsFrom(existing);
                metadata.UseDbFieldsFrom(existing.Metadata.Value);
            }
        }

        private static SeriesStatusType MapSeriesStatus(string status)
        {
            return status?.ToLower() switch
            {
                "ended" => SeriesStatusType.Ended,
                "cancelled" => SeriesStatusType.Ended,
                "hiatus" => SeriesStatusType.Continuing,
                _ => SeriesStatusType.Continuing
            };
        }

        private static SeriesType MapSeriesType(string seriesType)
        {
            return seriesType?.ToLower() switch
            {
                "ongoing" => SeriesType.Single,
                "mini-series" => SeriesType.Limited,
                "annual" => SeriesType.Annual,
                "tpb" or "trade paperback" => SeriesType.TPB,
                "hardcover" => SeriesType.Hardcover,
                "omnibus" => SeriesType.Omnibus,
                "one-shot" or "oneshot" => SeriesType.OneShot,
                "graphic novel" => SeriesType.GraphicNovel,
                _ => SeriesType.Single
            };
        }

        private static IssueType MapIssueType(string issueType)
        {
            return issueType?.ToLower() switch
            {
                "annual" => IssueType.Annual,
                "special" => IssueType.Special,
                "one-shot" or "oneshot" => IssueType.OneShot,
                "tpb" or "trade paperback" => IssueType.TPB,
                "hardcover" => IssueType.Hardcover,
                "omnibus" => IssueType.Omnibus,
                _ => IssueType.Standard
            };
        }
    }
}
