using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.Metron.Resources
{
    // Field names follow Metron's v1.0 API serializers
    // (github.com/Metron-Project/metron, api/v1_0/serializers).

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
    // List serializer fields: id, series, year_began, year_end, volume, issue_count, modified.
    // Note: no publisher in list responses.
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

        [JsonProperty("issue_count")]
        public int? IssueCount { get; set; }
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

        // Display string from Django IntegerChoices: Cancelled, Completed, Hiatus, Ongoing.
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
    // List serializer fields: id, series, number, issue, cover_date, store_date, image,
    // cover_hash, modified. "issue" is the composed display name ("Series (Year) #Num").
    public class MetronIssueListItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("issue")]
        public string DisplayName { get; set; }

        [JsonProperty("cover_date")]
        public DateTime? CoverDate { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }
    }

    public class MetronIssueDetail
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        // Collection title — set for TPBs/collections, usually null for single issues.
        [JsonProperty("title")]
        public string CollectionTitle { get; set; }

        // Story titles within the issue.
        [JsonProperty("name")]
        public List<string> StoryTitles { get; set; }

        [JsonProperty("cover_date")]
        public DateTime? CoverDate { get; set; }

        [JsonProperty("desc")]
        public string Description { get; set; }

        [JsonProperty("page")]
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
