using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class IssueAddedEvent : IEvent
    {
        public Issue Issue { get; private set; }
        public bool DoRefresh { get; private set; }

        public IssueAddedEvent(Issue issue, bool doRefresh = true)
        {
            Issue = issue;
            DoRefresh = doRefresh;
        }
    }
}
