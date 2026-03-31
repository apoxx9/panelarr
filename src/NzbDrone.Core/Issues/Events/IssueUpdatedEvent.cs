using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
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
