using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Issues.Commands
{
    public class RefreshIssueCommand : Command
    {
        public int? IssueId { get; set; }

        public RefreshIssueCommand()
        {
        }

        public RefreshIssueCommand(int? issueId)
        {
            IssueId = issueId;
        }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => !IssueId.HasValue;

        public override string CompletionMessage => "Completed";
    }
}
