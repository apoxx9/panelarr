using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Books.Commands
{
    public class RefreshBookCommand : Command
    {
        public int? IssueId { get; set; }

        public RefreshBookCommand()
        {
        }

        public RefreshBookCommand(int? bookId)
        {
            IssueId = bookId;
        }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => !IssueId.HasValue;

        public override string CompletionMessage => "Completed";
    }
}
