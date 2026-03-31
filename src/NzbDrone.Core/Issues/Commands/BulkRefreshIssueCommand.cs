using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Issues.Commands
{
    public class BulkRefreshIssueCommand : Command
    {
        public BulkRefreshIssueCommand()
        {
        }

        public BulkRefreshIssueCommand(List<int> bookIds)
        {
            IssueIds = bookIds;
        }

        public List<int> IssueIds { get; set; }

        public override bool SendUpdatesToClient => true;
    }
}
