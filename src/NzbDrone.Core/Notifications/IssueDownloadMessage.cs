using System.Collections.Generic;
using NzbDrone.Core.Download;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications
{
    public class IssueDownloadMessage
    {
        public string Message { get; set; }
        public Series Series { get; set; }
        public Issue Issue { get; set; }
        public List<ComicFile> ComicFiles { get; set; }
        public List<ComicFile> OldFiles { get; set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
