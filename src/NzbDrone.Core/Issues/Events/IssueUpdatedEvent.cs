using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Books.Events
{
    public class IssueUpdatedEvent : IEvent
    {
        public Issue Issue { get; private set; }

        public IssueUpdatedEvent(Issue issue)
        {
            Issue = issue;
        }
    }
}
