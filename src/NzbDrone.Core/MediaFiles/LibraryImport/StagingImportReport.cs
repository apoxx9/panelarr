using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles.LibraryImport
{
    // Per-file outcome of a staging import, kept so the UI can show why a
    // file stayed in the staging folder (docs/staging-folder-import.md).
    public class StagingImportFileResult
    {
        public string Path { get; set; }
        public string Series { get; set; }
        public StagingImportFileOutcome Outcome { get; set; }
        public List<string> Reasons { get; set; } = new List<string>();
    }

    public enum StagingImportFileOutcome
    {
        Imported = 0,
        Rejected = 1,
        Failed = 2
    }

    public class StagingImportReport
    {
        public int CommandId { get; set; }
        public List<StagingImportFileResult> Files { get; set; } = new List<StagingImportFileResult>();
        public List<string> Errors { get; set; } = new List<string>();
    }
}
