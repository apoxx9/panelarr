namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookComicFileDeletePayload : WebhookPayload
    {
        public WebhookSeries Series { get; set; }
        public WebhookIssue Issue { get; set; }
        public WebhookComicFile ComicFile { get; set; }
    }
}
