using System;
using System.Collections.Generic;
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
        public ParsedTrackInfo FileTrackInfo { get; set; }
        public ParsedIssueInfo FolderTrackInfo { get; set; }
        public ParsedIssueInfo DownloadClientIssueInfo { get; set; }
        public List<string> AcoustIdResults { get; set; }
        public Series Series { get; set; }
        public Issue Issue { get; set; }
        public Distance Distance { get; set; }
        public QualityModel Quality { get; set; }
        public IndexerFlags IndexerFlags { get; set; }
        public bool ExistingFile { get; set; }
        public bool AdditionalFile { get; set; }
        public bool SceneSource { get; set; }
        public string ReleaseGroup { get; set; }
        public string SceneName { get; set; }

        public override string ToString()
        {
            return Path;
        }
    }
}
