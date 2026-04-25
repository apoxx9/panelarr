using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Download;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class IssueImportedEvent : IEvent
    {
        public Series Series { get; private set; }
        public Issue Issue { get; private set; }
        public List<ComicFile> ImportedIssues { get; private set; }
        public List<ComicFile> OldFiles { get; private set; }
        public bool NewDownload { get; private set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; private set; }

        public IssueImportedEvent(Series series, Issue issue, List<ComicFile> importedIssues, List<ComicFile> oldFiles, bool newDownload, DownloadClientItem downloadClientItem)
        {
            Series = series;
            Issue = issue;
            ImportedIssues = importedIssues;
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
