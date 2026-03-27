namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRetagPayload : WebhookPayload
    {
        public WebhookSeries Series { get; set; }
        public WebhookBookFile ComicFile { get; set; }
    }
}
