using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class BulkBookResource
    {
        public List<WorkResource> Works { get; set; }
        public List<SeriesResource> SeriesGroup { get; set; }
        public List<SeriesResource> Seriess { get; set; }
    }
}
