using System;
using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class WorkResource
    {
        public int ForeignId { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public List<string> Genres { get; set; }
        public List<int> RelatedWorks { get; set; }
        public List<IssueResource> Books { get; set; }
        public List<SeriesResource> SeriesGroup { get; set; } = new List<SeriesResource>();
        public List<SeriesResource> Seriess { get; set; } = new List<SeriesResource>();
    }
}
