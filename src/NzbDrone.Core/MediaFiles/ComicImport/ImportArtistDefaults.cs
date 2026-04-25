using System.Collections.Generic;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.IssueImport
{
    public class ImportSeriesDefaults
    {
        public int MetadataProfileId { get; set; }
        public int LanguageProfileId { get; set; }
        public int QualityProfileId { get; set; }
        public bool IssueFolder { get; set; }
        public MonitorTypes Monitored { get; set; }
        public HashSet<int> Tags { get; set; }
    }
}
