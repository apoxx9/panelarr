using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class SeriesAddedEvent : IEvent
    {
        public Series Series { get; private set; }
        public bool DoRefresh { get; private set; }

        public SeriesAddedEvent(Series author, bool doRefresh = true)
        {
            Series = author;
            DoRefresh = doRefresh;
        }
    }
}
