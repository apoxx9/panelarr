using NzbDrone.Core.Books;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookSeries
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string ForeignSeriesId { get; set; }

        public WebhookSeries()
        {
        }

        public WebhookSeries(Series author)
        {
            Id = author.Id;
            Name = author.Name;
            Path = author.Path;
            ForeignSeriesId = author.Metadata.Value.ForeignSeriesId;
        }
    }
}
