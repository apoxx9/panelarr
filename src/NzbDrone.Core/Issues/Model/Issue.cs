using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Equ;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Issues
{
    [DebuggerDisplay("{GetType().FullName} ID = {Id} [{ForeignIssueId}][{Title}]")]
    public class Issue : Entity<Issue>
    {
        public Issue()
        {
            Links = new List<Links>();
            Genres = new List<string>();
            Credits = new List<Credit>();
            Ratings = new Ratings();
            Series = new Series();
            AddOptions = new AddIssueOptions();
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
        public string Overview { get; set; }
        public List<Credit> Credits { get; set; }

        // These are Panelarr generated/config
        public string CleanTitle { get; set; }
        public bool Monitored { get; set; }
        public bool IsOverridden { get; set; }
        public string OverriddenFields { get; set; }
        public DateTime? LastInfoSync { get; set; }
        public DateTime Added { get; set; }
        [MemberwiseEqualityIgnore]
        public AddIssueOptions AddOptions { get; set; }

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
            get { return Series?.Value?.Id ?? 0; }
            set { Series.Value.Id = value; }
        }

        public override string ToString()
        {
            return string.Format("[{0}][{1}]", ForeignIssueId, Title.NullSafe());
        }

        public override void UseMetadataFrom(Issue other)
        {
            var overridden = IsOverridden
                ? (OverriddenFields ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();

            if (!IsProtectedField(overridden, "ForeignIssueId"))
            {
                ForeignIssueId = other.ForeignIssueId;
            }

            if (!IsProtectedField(overridden, "TitleSlug"))
            {
                TitleSlug = other.TitleSlug;
            }

            if (!IsProtectedField(overridden, "Title"))
            {
                Title = other.Title;
            }

            if (!IsProtectedField(overridden, "ReleaseDate"))
            {
                ReleaseDate = other.ReleaseDate;
            }

            if (!IsProtectedField(overridden, "Links"))
            {
                Links = other.Links;
            }

            if (!IsProtectedField(overridden, "Genres"))
            {
                Genres = other.Genres;
            }

            if (!IsProtectedField(overridden, "Ratings"))
            {
                Ratings = other.Ratings;
            }

            if (!IsProtectedField(overridden, "CleanTitle"))
            {
                CleanTitle = other.CleanTitle;
            }

            if (!IsProtectedField(overridden, "IssueNumber"))
            {
                IssueNumber = other.IssueNumber;
            }

            if (!IsProtectedField(overridden, "IssueType"))
            {
                IssueType = other.IssueType;
            }

            if (!IsProtectedField(overridden, "CoverArtUrl"))
            {
                CoverArtUrl = other.CoverArtUrl;
            }

            if (!IsProtectedField(overridden, "PageCount"))
            {
                PageCount = other.PageCount;
            }

            if (!IsProtectedField(overridden, "Overview"))
            {
                Overview = other.Overview;
            }
        }

        public override void UseDbFieldsFrom(Issue other)
        {
            Id = other.Id;
            SeriesMetadataId = other.SeriesMetadataId;
            Monitored = other.Monitored;
            IsOverridden = other.IsOverridden;
            OverriddenFields = other.OverriddenFields;
            LastInfoSync = other.LastInfoSync;
            LastSearchTime = other.LastSearchTime;
            Added = other.Added;
            AddOptions = other.AddOptions;
            Credits = other.Credits;
        }

        public override void ApplyChanges(Issue other)
        {
            ForeignIssueId = other.ForeignIssueId;
            AddOptions = other.AddOptions;
            Monitored = other.Monitored;
        }

        private bool IsProtectedField(string[] overridden, string field)
        {
            return IsOverridden && Array.IndexOf(overridden, field) >= 0;
        }
    }
}
