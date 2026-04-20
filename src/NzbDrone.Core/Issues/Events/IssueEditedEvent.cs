using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class IssueEditedEvent : IEvent
    {
        public Issue Issue { get; private set; }
        public Issue OldIssue { get; private set; }

        public IssueEditedEvent(Issue issue, Issue oldIssue)
        {
            Issue = issue;
            OldIssue = oldIssue;
        }
    }
}
