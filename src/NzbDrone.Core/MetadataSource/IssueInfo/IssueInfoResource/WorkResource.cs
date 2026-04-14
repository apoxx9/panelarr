using System;
using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.IssueInfo
{
    public class WorkResource
    {
        public int ForeignId { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public List<string> Genres { get; set; }
        public List<int> RelatedWorks { get; set; }
        public List<IssueResource> Issues { get; set; }
        public List<SeriesResource> SeriesGroup { get; set; } = new List<SeriesResource>();
        public List<SeriesResource> Series { get; set; } = new List<SeriesResource>();
    }
}
