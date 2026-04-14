using System.Collections.Generic;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public interface INotification : IProvider
    {
        string Link { get; }

        void OnGrab(GrabMessage grabMessage);
        void OnReleaseImport(IssueDownloadMessage message);
        void OnRename(Series series, List<RenamedComicFile> renamedFiles);
        void OnSeriesAdded(Series series);
        void OnSeriesDelete(SeriesDeleteMessage deleteMessage);
        void OnIssueDelete(IssueDeleteMessage deleteMessage);
        void OnComicFileDelete(ComicFileDeleteMessage deleteMessage);
        void OnHealthIssue(HealthCheck.HealthCheck healthCheck);
        void OnApplicationUpdate(ApplicationUpdateMessage updateMessage);
        void OnDownloadFailure(DownloadFailedMessage message);
        void OnImportFailure(IssueDownloadMessage message);
        void OnIssueRetag(IssueRetagMessage message);
        void ProcessQueue();
        bool SupportsOnGrab { get; }
        bool SupportsOnReleaseImport { get; }
        bool SupportsOnUpgrade { get; }
        bool SupportsOnRename { get; }
        bool SupportsOnSeriesAdded { get; }
        bool SupportsOnSeriesDelete { get; }
        bool SupportsOnIssueDelete { get; }
        bool SupportsOnComicFileDelete { get; }
        bool SupportsOnComicFileDeleteForUpgrade { get; }
        bool SupportsOnHealthIssue { get; }
        bool SupportsOnApplicationUpdate { get; }
        bool SupportsOnDownloadFailure { get; }
        bool SupportsOnImportFailure { get; }
        bool SupportsOnIssueRetag { get; }
    }
}
