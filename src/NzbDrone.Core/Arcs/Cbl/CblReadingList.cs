using System.Collections.Generic;

namespace NzbDrone.Core.Arcs.Cbl
{
    public class CblReadingList
    {
        public string Name { get; set; }
        public List<CblBook> Books { get; set; } = new List<CblBook>();
    }

    public class CblBook
    {
        public string Series { get; set; }
        public string Number { get; set; }

        // Series start year by community convention.
        public string Volume { get; set; }
        public string Year { get; set; }

        // From the community <Database Name="cv" .../> extension.
        public int? CvVolumeId { get; set; }
        public int? CvIssueId { get; set; }
    }
}
