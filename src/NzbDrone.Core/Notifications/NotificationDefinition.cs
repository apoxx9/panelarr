using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public class NotificationDefinition : ProviderDefinition
    {
        public bool OnGrab { get; set; }
        public bool OnReleaseImport { get; set; }
        public bool OnUpgrade { get; set; }
        public bool OnRename { get; set; }
        public bool OnSeriesAdded { get; set; }
        public bool OnSeriesDelete { get; set; }
        public bool OnIssueDelete { get; set; }
        public bool OnComicFileDelete { get; set; }
        public bool OnComicFileDeleteForUpgrade { get; set; }
        public bool OnHealthIssue { get; set; }
        public bool OnDownloadFailure { get; set; }
        public bool OnImportFailure { get; set; }
        public bool OnIssueRetag { get; set; }
        public bool OnApplicationUpdate { get; set; }
        public bool SupportsOnGrab { get; set; }
        public bool SupportsOnReleaseImport { get; set; }
        public bool SupportsOnUpgrade { get; set; }
        public bool SupportsOnRename { get; set; }
        public bool SupportsOnSeriesAdded { get; set; }
        public bool SupportsOnSeriesDelete { get; set; }
        public bool SupportsOnIssueDelete { get; set; }
        public bool SupportsOnComicFileDelete { get; set; }
        public bool SupportsOnComicFileDeleteForUpgrade { get; set; }
        public bool SupportsOnHealthIssue { get; set; }
        public bool IncludeHealthWarnings { get; set; }
        public bool SupportsOnDownloadFailure { get; set; }
        public bool SupportsOnImportFailure { get; set; }
        public bool SupportsOnIssueRetag { get; set; }
        public bool SupportsOnApplicationUpdate { get; set; }

        public override bool Enable => OnGrab || OnReleaseImport || (OnReleaseImport && OnUpgrade) || OnRename || OnSeriesAdded || OnSeriesDelete || OnIssueDelete || OnComicFileDelete || OnComicFileDeleteForUpgrade || OnHealthIssue || OnDownloadFailure || OnImportFailure || OnIssueRetag || OnApplicationUpdate;
    }
}
