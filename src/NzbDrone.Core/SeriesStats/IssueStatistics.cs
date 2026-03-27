using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.SeriesStats
{
    public class IssueStatistics : ResultSet
    {
        public int SeriesId { get; set; }
        public int IssueId { get; set; }
        public int ComicFileCount { get; set; }
        public int IssueCount { get; set; }
        public int AvailableBookCount { get; set; }
        public int TotalBookCount { get; set; }
        public long SizeOnDisk { get; set; }
    }
}
