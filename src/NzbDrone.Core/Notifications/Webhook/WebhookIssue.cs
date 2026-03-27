using System;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookBook
    {
        public WebhookBook()
        {
        }

        public WebhookBook(Issue issue)
        {
            Id = issue.Id;
            GoodreadsId = issue.ForeignIssueId;
            Title = issue.Title;
            ReleaseDate = issue.ReleaseDate;
        }

        public int Id { get; set; }
        public string GoodreadsId { get; set; }
        public string Title { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }
}
