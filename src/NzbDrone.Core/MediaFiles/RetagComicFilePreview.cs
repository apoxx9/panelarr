using System;
using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles
{
    public class RetagComicFilePreview
    {
        public int SeriesId { get; set; }
        public int IssueId { get; set; }
        public List<int> TrackNumbers { get; set; } = new List<int>();
        public int ComicFileId { get; set; }
        public string Path { get; set; }
        public Dictionary<string, Tuple<string, string>> Changes { get; set; }
    }
}
