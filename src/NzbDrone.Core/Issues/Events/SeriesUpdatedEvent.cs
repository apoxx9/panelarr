using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class SeriesUpdatedEvent : IEvent
    {
        public Series Series { get; private set; }

        public SeriesUpdatedEvent(Series series)
        {
            Series = series;
        }
    }
}
