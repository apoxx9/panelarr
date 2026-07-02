using System;
using System.Collections.Generic;
using System.Globalization;

namespace NzbDrone.Core.MetadataSource.Provider
{
    public class ProviderSeries
    {
        public string ForeignSeriesId { get; set; }
        public string Name { get; set; }
        public string SortName { get; set; }
        public string Overview { get; set; }
        public string Status { get; set; }
        public string SeriesType { get; set; }
        public int? Year { get; set; }
        public int? VolumeNumber { get; set; }
        public string ForeignPublisherId { get; set; }
        public string PublisherName { get; set; }
        public int? IssueCount { get; set; }
        public List<ProviderIssue> Issues { get; set; }
        public List<string> Genres { get; set; }
        public string ImageUrl { get; set; }
    }

    public class ProviderIssue
    {
        public string ForeignIssueId { get; set; }

        // The parent series/volume, when the provider's issue payload carries
        // it (CV issue detail: "volume", Metron issue detail: "series"). Lets
        // an issue id found in file tags resolve to its series without the
        // series being in the library.
        public string ForeignSeriesId { get; set; }
        public string SeriesName { get; set; }

        // Kept as a string: comics use fractional and alpha numbers ("0.5", "1a", "1.MU").
        public string IssueNumber { get; set; }
        public string Title { get; set; }
        public string Overview { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string IssueType { get; set; }
        public int? PageCount { get; set; }
        public string CoverUrl { get; set; }
    }

    public class ProviderPublisher
    {
        public string ForeignPublisherId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
    }

    public static class IssueNumberNormalizer
    {
        // Canonicalizes numeric issue numbers ("003" -> "3", "0.50" -> "0.5") using the
        // same "0.##" form the identification candidate matching uses; non-numeric
        // numbers ("1a", "1.MU", "½") pass through untouched.
        public static string Normalize(string number)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                return null;
            }

            var trimmed = number.Trim();

            if (decimal.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
            {
                return n.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return trimmed;
        }
    }
}
