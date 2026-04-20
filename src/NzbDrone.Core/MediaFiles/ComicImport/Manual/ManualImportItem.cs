using System.Collections.Generic;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.IssueImport.Manual
{
    public class ManualImportItem : ModelBase
    {
        public ManualImportItem()
        {
            CustomFormats = new List<CustomFormat>();
        }

        public string Path { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public Series Series { get; set; }
        public Issue Issue { get; set; }
        public QualityModel Quality { get; set; }
        public string ReleaseGroup { get; set; }
        public string DownloadId { get; set; }
        public List<CustomFormat> CustomFormats { get; set; }
        public int IndexerFlags { get; set; }
        public IEnumerable<Rejection> Rejections { get; set; }
        public ParsedTrackInfo Tags { get; set; }
        public bool AdditionalFile { get; set; }
        public bool ReplaceExistingFiles { get; set; }
        public bool DisableReleaseSwitching { get; set; }
    }
}
