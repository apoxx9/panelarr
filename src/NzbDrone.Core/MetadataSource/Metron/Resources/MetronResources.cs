using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.Metron.Resources
{
    // --- Pagination wrapper ---
    public class MetronPagedResponse<T>
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("next")]
        public string Next { get; set; }

        [JsonProperty("previous")]
        public string Previous { get; set; }

        [JsonProperty("results")]
        public List<T> Results { get; set; }
    }

    // --- Series resources ---
    public class MetronSeriesListItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("series")]
        public string Name { get; set; }

        [JsonProperty("volume")]
        public int? Volume { get; set; }

        [JsonProperty("year_began")]
        public int? YearBegan { get; set; }

        [JsonProperty("publisher")]
        public MetronIdName Publisher { get; set; }
    }

    public class MetronSeriesDetail
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sort_name")]
        public string SortName { get; set; }

        [JsonProperty("volume")]
        public int? Volume { get; set; }

        [JsonProperty("year_began")]
        public int? YearBegan { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("series_type")]
        public MetronIdName SeriesType { get; set; }

        [JsonProperty("publisher")]
        public MetronIdName Publisher { get; set; }

        [JsonProperty("desc")]
        public string Description { get; set; }

        [JsonProperty("genres")]
        public List<MetronIdName> Genres { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }
    }

    // --- Issue resources ---
    public class MetronIssueListItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("issue_name")]
        public string IssueName { get; set; }

        [JsonProperty("cover_date")]
        public DateTime? CoverDate { get; set; }
    }

    public class MetronIssueDetail
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("issue_name")]
        public string IssueName { get; set; }

        [JsonProperty("cover_date")]
        public DateTime? CoverDate { get; set; }

        [JsonProperty("desc")]
        public string Description { get; set; }

        [JsonProperty("page_count")]
        public int? PageCount { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("series")]
        public MetronIdName Series { get; set; }
    }

    // --- Publisher resources ---
    public class MetronPublisherDetail
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("desc")]
        public string Description { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }
    }

    // --- Common ---
    public class MetronIdName
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
