namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookBookFileDeletePayload : WebhookPayload
    {
        public WebhookSeries Series { get; set; }
        public WebhookBook Issue { get; set; }
        public WebhookBookFile ComicFile { get; set; }
    }
}
