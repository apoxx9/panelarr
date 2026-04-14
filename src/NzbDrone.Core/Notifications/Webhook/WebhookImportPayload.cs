using System.Collections.Generic;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookImportPayload : WebhookPayload
    {
        public WebhookSeries Series { get; set; }
        public WebhookIssue Issue { get; set; }
        public List<WebhookComicFile> ComicFiles { get; set; }
        public List<WebhookComicFile> DeletedFiles { get; set; }
        public bool IsUpgrade { get; set; }
        public string DownloadClient { get; set; }
        public string DownloadClientType { get; set; }
        public string DownloadId { get; set; }
    }
}
