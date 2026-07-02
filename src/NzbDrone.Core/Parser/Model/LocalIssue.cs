using System;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalIssue
    {
        public string Path { get; set; }
        public int Part { get; set; }
        public int PartCount { get; set; }
        public long Size { get; set; }
        public DateTime Modified { get; set; }
        public ParsedFileTagInfo FileTagInfo { get; set; }
        public ParsedIssueInfo FolderTrackInfo { get; set; }
        public ParsedIssueInfo DownloadClientIssueInfo { get; set; }
        public Series Series { get; set; }
        public Issue Issue { get; set; }
        public Distance Distance { get; set; }
        public QualityModel Quality { get; set; }
        public IndexerFlags IndexerFlags { get; set; }
        public bool ExistingFile { get; set; }
        public bool AdditionalFile { get; set; }

        // True when identified via an embedded provider issue id that resolved
        // to a library issue — the only match kind scans may auto-import from
        // folders outside a series path
        public bool ExactTagMatch { get; set; }
        public bool SceneSource { get; set; }
        public string ReleaseGroup { get; set; }
        public string SceneName { get; set; }

        public override string ToString()
        {
            return Path;
        }
    }
}
