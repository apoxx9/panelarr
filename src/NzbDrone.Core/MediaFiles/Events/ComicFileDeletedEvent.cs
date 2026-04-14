using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class ComicFileDeletedEvent : IEvent
    {
        public ComicFile ComicFile { get; private set; }
        public DeleteMediaFileReason Reason { get; private set; }

        public ComicFileDeletedEvent(ComicFile comicFile, DeleteMediaFileReason reason)
        {
            ComicFile = comicFile;
            Reason = reason;
        }
    }
}
