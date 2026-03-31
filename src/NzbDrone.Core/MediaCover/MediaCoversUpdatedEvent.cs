using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaCover
{
    public class MediaCoversUpdatedEvent : IEvent
    {
        public Series Series { get; set; }
        public Issue Issue { get; set; }

        public MediaCoversUpdatedEvent(Series series)
        {
            Series = series;
        }

        public MediaCoversUpdatedEvent(Issue issue)
        {
            Issue = issue;
        }
    }
}
