using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.IssueInfo
{
    public class BulkIssueResource
    {
        public List<WorkResource> Works { get; set; }
        public List<SeriesResource> SeriesGroup { get; set; }
        public List<SeriesResource> Series { get; set; }
    }
}
