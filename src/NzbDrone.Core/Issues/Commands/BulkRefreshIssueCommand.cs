using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Books.Commands
{
    public class BulkRefreshBookCommand : Command
    {
        public BulkRefreshBookCommand()
        {
        }

        public BulkRefreshBookCommand(List<int> bookIds)
        {
            IssueIds = bookIds;
        }

        public List<int> IssueIds { get; set; }

        public override bool SendUpdatesToClient => true;
    }
}
