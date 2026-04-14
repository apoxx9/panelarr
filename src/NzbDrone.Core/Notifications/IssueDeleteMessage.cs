using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Notifications
{
    public class IssueDeleteMessage
    {
        public string Message { get; set; }
        public Issue Issue { get; set; }
        public bool DeletedFiles { get; set; }
        public string DeletedFilesMessage { get; set; }

        public override string ToString()
        {
            return Message;
        }

        public IssueDeleteMessage(Issue issue, bool deleteFiles)
        {
            Issue = issue;
            DeletedFiles = deleteFiles;
            DeletedFilesMessage = DeletedFiles ?
                "Issue removed and all files were deleted" :
                "Issue removed, files were not deleted";
            Message = issue.Title + " - " + DeletedFilesMessage;
        }
    }
}
