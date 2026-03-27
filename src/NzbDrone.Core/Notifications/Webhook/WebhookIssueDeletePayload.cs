namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookBookDeletePayload : WebhookPayload
    {
        public WebhookSeries Series { get; set; }
        public WebhookBook Issue { get; set; }
        public bool DeletedFiles { get; set; }
    }
}
