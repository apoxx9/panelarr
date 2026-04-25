using System;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookIssue
    {
        public WebhookIssue()
        {
        }

        public WebhookIssue(Issue issue)
        {
            Id = issue.Id;
            ForeignIssueId = issue.ForeignIssueId;
            Title = issue.Title;
            ReleaseDate = issue.ReleaseDate;
        }

        public int Id { get; set; }
        public string ForeignIssueId { get; set; }
        public string Title { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }
}
