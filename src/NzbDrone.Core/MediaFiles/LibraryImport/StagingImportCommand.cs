using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.LibraryImport
{
    // Staging import: identify series in a folder OUTSIDE the library and
    // TRANSFER their files into the target root folder, renamed per the
    // naming settings. Contrast with LibraryImportCommand, which adopts
    // in-place folders and never touches files (docs/staging-folder-import.md).
    public class StagingImportCommand : Command
    {
        public StagingImportCommand()
        {
            Series = new List<LibraryImportSeries>();
        }

        public List<LibraryImportSeries> Series { get; set; }
        public int QualityProfileId { get; set; }
        public bool Monitored { get; set; } = true;
        public string MonitorNewItems { get; set; }
        public string RootFolderPath { get; set; }
        public bool KeepSourceFiles { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;
    }
}
