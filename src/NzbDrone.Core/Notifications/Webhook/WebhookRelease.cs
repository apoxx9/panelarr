using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRelease
    {
        public WebhookRelease()
        {
        }

        public WebhookRelease(QualityModel quality, RemoteIssue remoteIssue)
        {
            Quality = quality.Quality.Name;
            QualityVersion = quality.Revision.Version;
            ReleaseGroup = remoteIssue.ParsedIssueInfo.ReleaseGroup;
            ReleaseTitle = remoteIssue.Release.Title;
            Indexer = remoteIssue.Release.Indexer;
            Size = remoteIssue.Release.Size;
            CustomFormats = remoteIssue.CustomFormats?.Select(x => x.Name).ToList();
            CustomFormatScore = remoteIssue.CustomFormatScore;
        }

        public string Quality { get; set; }
        public int QualityVersion { get; set; }
        public string ReleaseGroup { get; set; }
        public string ReleaseTitle { get; set; }
        public string Indexer { get; set; }
        public long Size { get; set; }
        public int CustomFormatScore { get; set; }
        public List<string> CustomFormats { get; set; }
    }
}
