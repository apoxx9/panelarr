using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Equ;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Books
{
    [DebuggerDisplay("{GetType().FullName} ID = {Id} [{ForeignIssueId}][{Title}]")]
    public class Issue : Entity<Issue>
    {
        public Issue()
        {
            Links = new List<Links>();
            Genres = new List<string>();
            Ratings = new Ratings();
            Series = new Series();
            AddOptions = new AddBookOptions();
        }

        // These correspond to columns in the Issues table
        // These are metadata entries
        public int SeriesMetadataId { get; set; }
        public string ForeignIssueId { get; set; }
        public string TitleSlug { get; set; }
        public string Title { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public List<Links> Links { get; set; }
        public List<string> Genres { get; set; }
        public Ratings Ratings { get; set; }
        public DateTime? LastSearchTime { get; set; }

        // Comic-specific fields
        public float IssueNumber { get; set; }
        public IssueType IssueType { get; set; }
        public string CoverArtUrl { get; set; }
        public int PageCount { get; set; }

        // These are Panelarr generated/config
        public string CleanTitle { get; set; }
        public bool Monitored { get; set; }
        public DateTime? LastInfoSync { get; set; }
        public DateTime Added { get; set; }
        [MemberwiseEqualityIgnore]
        public AddBookOptions AddOptions { get; set; }

        // These are dynamically queried from other tables
        [MemberwiseEqualityIgnore]
        public LazyLoaded<SeriesMetadata> SeriesMetadata { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<Series> Series { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<List<ComicFile>> ComicFiles { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<List<SeriesGroupLink>> SeriesLinks { get; set; }

        //compatibility properties with old version of Issue
        [MemberwiseEqualityIgnore]
        [JsonIgnore]
        public int SeriesId
        {
            get { return Series?.Value?.Id ?? 0; } set { Series.Value.Id = value; }
        }

        public override string ToString()
        {
            return string.Format("[{0}][{1}]", ForeignIssueId, Title.NullSafe());
        }

        public override void UseMetadataFrom(Issue other)
        {
            ForeignIssueId = other.ForeignIssueId;
            TitleSlug = other.TitleSlug;
            Title = other.Title;
            ReleaseDate = other.ReleaseDate;
            Links = other.Links;
            Genres = other.Genres;
            Ratings = other.Ratings;
            CleanTitle = other.CleanTitle;
            IssueNumber = other.IssueNumber;
            IssueType = other.IssueType;
            CoverArtUrl = other.CoverArtUrl;
            PageCount = other.PageCount;
        }

        public override void UseDbFieldsFrom(Issue other)
        {
            Id = other.Id;
            SeriesMetadataId = other.SeriesMetadataId;
            Monitored = other.Monitored;
            LastInfoSync = other.LastInfoSync;
            LastSearchTime = other.LastSearchTime;
            Added = other.Added;
            AddOptions = other.AddOptions;
        }

        public override void ApplyChanges(Issue other)
        {
            ForeignIssueId = other.ForeignIssueId;
            AddOptions = other.AddOptions;
            Monitored = other.Monitored;
        }
    }
}
