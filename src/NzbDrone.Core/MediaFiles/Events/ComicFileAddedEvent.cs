using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class ComicFileAddedEvent : IEvent
    {
        public ComicFile ComicFile { get; private set; }

        public ComicFileAddedEvent(ComicFile comicFile)
        {
            ComicFile = comicFile;
        }
    }
}
