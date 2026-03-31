using NzbDrone.Common.Messaging;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaCover
{
    public class MediaCoversUpdatedEvent : IEvent
    {
        public Series Series { get; set; }
        public Issue Issue { get; set; }

        public MediaCoversUpdatedEvent(Series author)
        {
            Series = author;
        }

        public MediaCoversUpdatedEvent(Issue issue)
        {
            Issue = issue;
        }
    }
}
