using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Exceptions
{
    public class IssueNotFoundException : NzbDroneException
    {
        public string ForeignIssueId { get; set; }

        public IssueNotFoundException(string foreignIssueId)
            : base($"Issue with id {foreignIssueId} was not found, it may have been removed from metadata server.")
        {
            ForeignIssueId = foreignIssueId;
        }

        public IssueNotFoundException(string foreignIssueId, string message, params object[] args)
            : base(message, args)
        {
            ForeignIssueId = foreignIssueId;
        }

        public IssueNotFoundException(string foreignIssueId, string message)
            : base(message)
        {
            ForeignIssueId = foreignIssueId;
        }
    }
}
