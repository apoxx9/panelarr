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
                VolumeNumber = providerSeries.VolumeNumber,
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
                IssueNumber = providerIssue.IssueNumber ?? string.Empty,
                SortOrder = ParseSortOrder(providerIssue.IssueNumber),
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

        // Numeric sort key matching migration 009's backfill (old float issue
        // numbers); non-numeric numbers ("1a") sort by their leading number.
        private static float ParseSortOrder(string issueNumber)
        {
            if (string.IsNullOrWhiteSpace(issueNumber))
            {
                return 0;
            }

            if (float.TryParse(issueNumber, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n))
            {
                return n;
            }

            var leading = new string(issueNumber.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());

            return float.TryParse(leading, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ln) ? ln : 0;
        }

        // Metron statuses are Django choice display strings:
        // Cancelled, Completed, Hiatus, Ongoing.
        private static SeriesStatusType MapSeriesStatus(string status)
        {
            return status?.ToLower() switch
            {
                "ended" => SeriesStatusType.Ended,
                "cancelled" => SeriesStatusType.Ended,
                "completed" => SeriesStatusType.Ended,
                "hiatus" => SeriesStatusType.Continuing,
                _ => SeriesStatusType.Continuing
            };
        }

        // Metron series types (comicsdb/fixtures/series_type.yaml): One-Shot,
        // Annual Series, Hardcover, Graphic Novel, Trade Paperback, Limited Series,
        // Digital Chapters, Single Issue, Omnibus.
        private static SeriesType MapSeriesType(string seriesType)
        {
            return seriesType?.ToLower() switch
            {
                "single issue" or "ongoing" => SeriesType.Single,
                "limited series" or "mini-series" => SeriesType.Limited,
                "annual series" or "annual" => SeriesType.Annual,
                "tpb" or "trade paperback" => SeriesType.TPB,
                "hardcover" => SeriesType.Hardcover,
                "omnibus" => SeriesType.Omnibus,
                "one-shot" or "oneshot" => SeriesType.OneShot,
                "graphic novel" => SeriesType.GraphicNovel,
                "digital chapters" => SeriesType.Single,
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
