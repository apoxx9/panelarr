using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.ImportLists
{
    public class ImportListSyncCompleteEvent : IEvent
    {
        public List<Issue> ProcessedDecisions { get; private set; }

        public ImportListSyncCompleteEvent(List<Issue> processedDecisions)
        {
            ProcessedDecisions = processedDecisions;
        }
    }
}
