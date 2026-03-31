namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRetagPayload : WebhookPayload
    {
        public WebhookSeries Series { get; set; }
        public WebhookComicFile ComicFile { get; set; }
    }
}
