using NzbDrone.Common.Messaging;
using NzbDrone.Core.Download.TrackedDownloads;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class IssueImportIncompleteEvent : IEvent
    {
        public TrackedDownload TrackedDownload { get; private set; }

        public IssueImportIncompleteEvent(TrackedDownload trackedDownload)
        {
            TrackedDownload = trackedDownload;
        }
    }
}
