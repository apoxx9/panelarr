namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookSeriesAddedPayload : WebhookPayload
    {
        public WebhookSeries Series { get; set; }
    }
}
