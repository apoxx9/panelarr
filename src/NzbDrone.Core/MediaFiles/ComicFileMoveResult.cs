using System.Collections.Generic;

namespace NzbDrone.Core.MediaFiles
{
    public class ComicFileMoveResult
    {
        public ComicFileMoveResult()
        {
            OldFiles = new List<ComicFile>();
        }

        public ComicFile ComicFile { get; set; }
        public List<ComicFile> OldFiles { get; set; }
    }
}
