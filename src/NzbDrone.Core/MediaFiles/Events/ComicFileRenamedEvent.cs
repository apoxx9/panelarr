using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class ComicFileRenamedEvent : IEvent
    {
        public Series Series { get; private set; }
        public ComicFile ComicFile { get; private set; }
        public string OriginalPath { get; private set; }

        public ComicFileRenamedEvent(Series author, ComicFile comicFile, string originalPath)
        {
            Series = author;
            ComicFile = comicFile;
            OriginalPath = originalPath;
        }
    }
}
