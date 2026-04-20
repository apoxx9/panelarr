using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class TrackImportedEvent : IEvent
    {
        public LocalIssue IssueInfo { get; private set; }
        public ComicFile ImportedIssue { get; private set; }
        public List<ComicFile> OldFiles { get; private set; }
        public bool NewDownload { get; private set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; private set; }

        public TrackImportedEvent(LocalIssue issueInfo, ComicFile importedIssue, List<ComicFile> oldFiles, bool newDownload, DownloadClientItem downloadClientItem)
        {
            IssueInfo = issueInfo;
            ImportedIssue = importedIssue;
            OldFiles = oldFiles;
            NewDownload = newDownload;

            if (downloadClientItem != null)
            {
                DownloadClientInfo = downloadClientItem.DownloadClientInfo;
                DownloadId = downloadClientItem.DownloadId;
            }
        }
    }
}
