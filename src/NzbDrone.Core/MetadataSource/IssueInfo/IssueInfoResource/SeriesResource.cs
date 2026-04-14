using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.IssueInfo
{
    public class SeriesLinkItemResource
    {
        public long ForeignWorkId { get; set; }
        public string PositionInSeries { get; set; }
        public int SeriesPosition { get; set; }
        public bool Primary { get; set; }
    }

    public class SeriesResource
    {
        public int ForeignId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }
        public int RatingCount { get; set; }
        public double AverageRating { get; set; }
        public List<WorkResource> Works { get; set; }
        public List<SeriesResource> SeriesGroup { get; set; }
        public List<SeriesLinkItemResource> LinkItems { get; set; } = new List<SeriesLinkItemResource>();
    }
}
