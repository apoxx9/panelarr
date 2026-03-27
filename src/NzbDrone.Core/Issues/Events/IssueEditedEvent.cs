using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Books.Events
{
    public class IssueEditedEvent : IEvent
    {
        public Issue Issue { get; private set; }
        public Issue OldBook { get; private set; }

        public IssueEditedEvent(Issue issue, Issue oldBook)
        {
            Issue = issue;
            OldBook = oldBook;
        }
    }
}
