using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class IssueDeletedEvent : IEvent
    {
        public Issue Issue { get; private set; }
        public bool DeleteFiles { get; private set; }
        public bool AddImportListExclusion { get; private set; }

        public IssueDeletedEvent(Issue issue, bool deleteFiles, bool addImportListExclusion)
        {
            Issue = issue;
            DeleteFiles = deleteFiles;
            AddImportListExclusion = addImportListExclusion;
        }
    }
}
