using System;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class ComicFileImportFailedEvent : IEvent
    {
        public Exception Exception { get; set; }
        public LocalIssue IssueInfo { get; }
        public bool NewDownload { get; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; }
        public string DownloadId { get; }

        public ComicFileImportFailedEvent(Exception exception, LocalIssue issueInfo, bool newDownload, DownloadClientItem downloadClientItem)
        {
            Exception = exception;
            IssueInfo = issueInfo;
            NewDownload = newDownload;

            if (downloadClientItem != null)
            {
                DownloadClientInfo = downloadClientItem.DownloadClientInfo;
                DownloadId = downloadClientItem.DownloadId;
            }
        }
    }
}
