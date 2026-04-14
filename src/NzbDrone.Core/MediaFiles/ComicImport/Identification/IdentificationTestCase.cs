using System.Collections.Generic;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Metadata;

namespace NzbDrone.Core.MediaFiles.IssueImport.Identification
{
    public class BasicLocalTrack
    {
        public string Path { get; set; }
        public ParsedTrackInfo FileTrackInfo { get; set; }
    }

    public class SeriesTestCase
    {
        public string Series { get; set; }
        public MetadataProfile MetadataProfile { get; set; }
    }

    public class IdTestCase
    {
        public List<string> ExpectedMusicBrainzReleaseIds { get; set; }
        public List<SeriesTestCase> LibrarySeries { get; set; }
        public string Series { get; set; }
        public string Issue { get; set; }
        public string Release { get; set; }
        public bool NewDownload { get; set; }
        public bool SingleRelease { get; set; }
        public List<BasicLocalTrack> Tracks { get; set; }
    }
}
