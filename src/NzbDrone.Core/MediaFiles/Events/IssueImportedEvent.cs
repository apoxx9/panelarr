using System.Collections.Generic;
using NzbDrone.Common.Messaging;
using NzbDrone.Core.Books;
using NzbDrone.Core.Download;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class IssueImportedEvent : IEvent
    {
        public Series Series { get; private set; }
        public Issue Issue { get; private set; }
        public List<ComicFile> ImportedBooks { get; private set; }
        public List<ComicFile> OldFiles { get; private set; }
        public bool NewDownload { get; private set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; private set; }

        public IssueImportedEvent(Series author, Issue issue, List<ComicFile> importedBooks, List<ComicFile> oldFiles, bool newDownload, DownloadClientItem downloadClientItem)
        {
            Series = author;
            Issue = issue;
            ImportedBooks = importedBooks;
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
