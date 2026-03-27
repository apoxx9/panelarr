using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Books.Events
{
    public class SeriesUpdatedEvent : IEvent
    {
        public Series Series { get; private set; }

        public SeriesUpdatedEvent(Series author)
        {
            Series = author;
        }
    }
}
