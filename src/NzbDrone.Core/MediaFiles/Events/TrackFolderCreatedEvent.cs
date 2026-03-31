using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class TrackFolderCreatedEvent : IEvent
    {
        public Series Series { get; private set; }
        public ComicFile ComicFile { get; private set; }
        public string SeriesFolder { get; set; }
        public string IssueFolder { get; set; }
        public string TrackFolder { get; set; }

        public TrackFolderCreatedEvent(Series author, ComicFile comicFile)
        {
            Series = author;
            ComicFile = comicFile;
        }
    }
}
