using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class SeriesAddedEvent : IEvent
    {
        public Series Series { get; private set; }
        public bool DoRefresh { get; private set; }

        public SeriesAddedEvent(Series series, bool doRefresh = true)
        {
            Series = series;
            DoRefresh = doRefresh;
        }
    }
}
