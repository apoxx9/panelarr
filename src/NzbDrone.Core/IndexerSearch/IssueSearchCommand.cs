using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class IssueSearchCommand : Command
    {
        public List<int> IssueIds { get; set; }

        public override bool SendUpdatesToClient => true;

        public IssueSearchCommand()
        {
        }

        public IssueSearchCommand(List<int> issueIds)
        {
            IssueIds = issueIds;
        }
    }
}
