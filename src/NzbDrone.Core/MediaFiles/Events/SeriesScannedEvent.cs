using NzbDrone.Common.Messaging;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class SeriesScannedEvent : IEvent
    {
        public Series Series { get; private set; }

        public SeriesScannedEvent(Series author)
        {
            Series = author;
        }
    }
}
