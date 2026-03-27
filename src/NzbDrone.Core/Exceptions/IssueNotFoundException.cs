using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Exceptions
{
    public class IssueNotFoundException : NzbDroneException
    {
        public string ForeignIssueId { get; set; }

        public IssueNotFoundException(string foreignBookId)
            : base($"Issue with id {foreignBookId} was not found, it may have been removed from metadata server.")
        {
            ForeignIssueId = foreignBookId;
        }

        public IssueNotFoundException(string foreignBookId, string message, params object[] args)
            : base(message, args)
        {
            ForeignIssueId = foreignBookId;
        }

        public IssueNotFoundException(string foreignBookId, string message)
            : base(message)
        {
            ForeignIssueId = foreignBookId;
        }
    }
}
