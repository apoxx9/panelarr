using System.Collections.Generic;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookImportPayload : WebhookPayload
    {
        public WebhookSeries Series { get; set; }
        public WebhookBook Issue { get; set; }
        public List<WebhookBookFile> ComicFiles { get; set; }
        public List<WebhookBookFile> DeletedFiles { get; set; }
        public bool IsUpgrade { get; set; }
        public string DownloadClient { get; set; }
        public string DownloadClientType { get; set; }
        public string DownloadId { get; set; }
    }
}
