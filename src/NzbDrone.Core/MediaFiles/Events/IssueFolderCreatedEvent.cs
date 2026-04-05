using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class IssueFolderCreatedEvent : IEvent
    {
        public Series Series { get; private set; }
        public ComicFile ComicFile { get; private set; }
        public string SeriesFolder { get; set; }
        public string IssueFolder { get; set; }
        public string ComicFileFolder { get; set; }

        public IssueFolderCreatedEvent(Series series, ComicFile comicFile)
        {
            Series = series;
            ComicFile = comicFile;
        }
    }
}
