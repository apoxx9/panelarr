using NzbDrone.Core.Issues;

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

        public WebhookSeries(Series series)
        {
            Id = series.Id;
            Name = series.Name;
            Path = series.Path;
            ForeignSeriesId = series.Metadata.Value.ForeignSeriesId;
        }
    }
}
