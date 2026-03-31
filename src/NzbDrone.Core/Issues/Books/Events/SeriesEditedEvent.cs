using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class SeriesEditedEvent : IEvent
    {
        public Series Series { get; private set; }
        public Series OldSeries { get; private set; }

        public SeriesEditedEvent(Series author, Series oldSeries)
        {
            Series = author;
            OldSeries = oldSeries;
        }
    }
}
