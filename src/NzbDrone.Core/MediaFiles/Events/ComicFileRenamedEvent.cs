using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class ComicFileRenamedEvent : IEvent
    {
        public Series Series { get; private set; }
        public ComicFile ComicFile { get; private set; }
        public string OriginalPath { get; private set; }

        public ComicFileRenamedEvent(Series series, ComicFile comicFile, string originalPath)
        {
            Series = series;
            ComicFile = comicFile;
            OriginalPath = originalPath;
        }
    }
}
