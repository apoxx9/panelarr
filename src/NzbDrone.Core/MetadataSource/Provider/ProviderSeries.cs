using System;
using System.Collections.Generic;

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
        public int? IssueNumber { get; set; }
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
}
