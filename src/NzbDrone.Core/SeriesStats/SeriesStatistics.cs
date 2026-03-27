using System.Collections.Generic;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.SeriesStats
{
    public class SeriesStatistics : ResultSet
    {
        public int SeriesId { get; set; }
        public int ComicFileCount { get; set; }
        public int IssueCount { get; set; }
        public int AvailableBookCount { get; set; }
        public int TotalBookCount { get; set; }
        public long SizeOnDisk { get; set; }
        public List<IssueStatistics> IssueStatistics { get; set; }
    }
}
