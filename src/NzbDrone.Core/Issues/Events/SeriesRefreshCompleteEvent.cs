using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class SeriesRefreshCompleteEvent : IEvent
    {
        public Series Series { get; set; }

        public SeriesRefreshCompleteEvent(Series series)
        {
            Series = series;
        }
    }
}
