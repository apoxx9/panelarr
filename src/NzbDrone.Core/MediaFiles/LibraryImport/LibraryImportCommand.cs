using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.LibraryImport
{
    public class LibraryImportSeries
    {
        public string ForeignSeriesId { get; set; }
        public string Folder { get; set; }
    }

    public class LibraryImportCommand : Command
    {
        public List<LibraryImportSeries> Series { get; set; }

        // Review-screen globals: one set of settings for the whole batch
        public int QualityProfileId { get; set; }
        public int MetadataProfileId { get; set; }
        public bool Monitored { get; set; }
        public string MonitorNewItems { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;
    }
}
