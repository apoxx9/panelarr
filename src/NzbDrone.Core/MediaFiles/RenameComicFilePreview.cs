using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles
{
    public class RenameComicFilePreview
    {
        public int SeriesId { get; set; }
        public int IssueId { get; set; }
        public List<int> TrackNumbers { get; set; }
        public int ComicFileId { get; set; }
        public string ExistingPath { get; set; }
        public string NewPath { get; set; }
    }
}
