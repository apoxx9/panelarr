using System.Collections.Generic;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public interface INotification : IProvider
    {
        string Link { get; }

        void OnGrab(GrabMessage grabMessage);
        void OnReleaseImport(IssueDownloadMessage message);
        void OnRename(Series author, List<RenamedComicFile> renamedFiles);
        void OnSeriesAdded(Series author);
        void OnSeriesDelete(SeriesDeleteMessage deleteMessage);
        void OnBookDelete(IssueDeleteMessage deleteMessage);
        void OnBookFileDelete(ComicFileDeleteMessage deleteMessage);
        void OnHealthIssue(HealthCheck.HealthCheck healthCheck);
        void OnApplicationUpdate(ApplicationUpdateMessage updateMessage);
        void OnDownloadFailure(DownloadFailedMessage message);
        void OnImportFailure(IssueDownloadMessage message);
        void OnBookRetag(IssueRetagMessage message);
        void ProcessQueue();
        bool SupportsOnGrab { get; }
        bool SupportsOnReleaseImport { get; }
        bool SupportsOnUpgrade { get; }
        bool SupportsOnRename { get; }
        bool SupportsOnSeriesAdded { get; }
        bool SupportsOnSeriesDelete { get; }
        bool SupportsOnBookDelete { get; }
        bool SupportsOnBookFileDelete { get; }
        bool SupportsOnBookFileDeleteForUpgrade { get; }
        bool SupportsOnHealthIssue { get; }
        bool SupportsOnApplicationUpdate { get; }
        bool SupportsOnDownloadFailure { get; }
        bool SupportsOnImportFailure { get; }
        bool SupportsOnBookRetag { get; }
    }
}
