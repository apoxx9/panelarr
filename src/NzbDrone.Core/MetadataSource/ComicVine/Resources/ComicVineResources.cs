using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.ComicVine.Resources
{
    // --- Top-level API response wrapper ---
    public class ComicVineResponse<T>
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("number_of_page_results")]
        public int NumberOfPageResults { get; set; }

        [JsonProperty("number_of_total_results")]
        public int NumberOfTotalResults { get; set; }

        [JsonProperty("status_code")]
        public int StatusCode { get; set; }

        [JsonProperty("results")]
        public T Results { get; set; }
    }

    // --- Volume (Series) resources ---
    public class ComicVineVolumeSummary
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("start_year")]
        public string StartYear { get; set; }

        [JsonProperty("publisher")]
        public ComicVineIdName Publisher { get; set; }

        [JsonProperty("image")]
        public ComicVineImage Image { get; set; }

        [JsonProperty("count_of_issues")]
        public int CountOfIssues { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("deck")]
        public string Deck { get; set; }

        [JsonProperty("date_last_updated")]
        public DateTime? DateLastUpdated { get; set; }
    }

    public class ComicVineVolumeDetail : ComicVineVolumeSummary
    {
        [JsonProperty("description")]
        public new string Description { get; set; }

        [JsonProperty("issues")]
        public List<ComicVineIssueSummary> Issues { get; set; }
    }

    // --- Issue resources ---
    public class ComicVineIssueSummary
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("issue_number")]
        public string IssueNumber { get; set; }

        [JsonProperty("cover_date")]
        public string CoverDate { get; set; }

        [JsonProperty("image")]
        public ComicVineImage Image { get; set; }
    }

    public class ComicVineIssueDetail : ComicVineIssueSummary
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("volume")]
        public ComicVineIdName Volume { get; set; }
    }

    // --- Publisher resources ---
    public class ComicVinePublisherDetail
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("image")]
        public ComicVineImage Image { get; set; }
    }

    // --- Story arcs ---
    public class ComicVineStoryArcSummary
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("deck")]
        public string Deck { get; set; }

        [JsonProperty("publisher")]
        public ComicVineIdName Publisher { get; set; }

        [JsonProperty("count_of_issue_appearances")]
        public int CountOfIssueAppearances { get; set; }

        // Detail-only: unordered {id, name} pairs — the ids are hydrated via
        // /issues?filter=id:... because CV stores no order, numbers or dates
        // on arc membership (docs/story-arcs.md).
        [JsonProperty("issues")]
        public List<ComicVineIdName> Issues { get; set; }
    }

    // Row of /issues?filter=story_arc:{id} — the only CV surface that gives
    // an arc's issues WITH numbers and dates (the story_arc detail's own
    // issues array is unordered {id, name} pairs; see docs/story-arcs.md).
    public class ComicVineArcIssue
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("issue_number")]
        public string IssueNumber { get; set; }

        [JsonProperty("cover_date")]
        public string CoverDate { get; set; }

        [JsonProperty("volume")]
        public ComicVineIdName Volume { get; set; }
    }

    // --- Shared helpers ---
    public class ComicVineIdName
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class ComicVineImage
    {
        [JsonProperty("medium_url")]
        public string MediumUrl { get; set; }

        [JsonProperty("original_url")]
        public string OriginalUrl { get; set; }
    }
}
