using NzbDrone.Core.Notifications;

namespace Panelarr.Api.V1.Notifications
{
    public class NotificationResource : ProviderResource<NotificationResource>
    {
        public string Link { get; set; }
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
        public string TestCommand { get; set; }
    }

    public class NotificationResourceMapper : ProviderResourceMapper<NotificationResource, NotificationDefinition>
    {
        public override NotificationResource ToResource(NotificationDefinition definition)
        {
            if (definition == null)
            {
                return default(NotificationResource);
            }

            var resource = base.ToResource(definition);

            resource.OnGrab = definition.OnGrab;
            resource.OnReleaseImport = definition.OnReleaseImport;
            resource.OnUpgrade = definition.OnUpgrade;
            resource.OnRename = definition.OnRename;
            resource.OnSeriesAdded = definition.OnSeriesAdded;
            resource.OnSeriesDelete = definition.OnSeriesDelete;
            resource.OnIssueDelete = definition.OnIssueDelete;
            resource.OnComicFileDelete = definition.OnComicFileDelete;
            resource.OnComicFileDeleteForUpgrade = definition.OnComicFileDeleteForUpgrade;
            resource.OnHealthIssue = definition.OnHealthIssue;
            resource.OnDownloadFailure = definition.OnDownloadFailure;
            resource.OnImportFailure = definition.OnImportFailure;
            resource.OnIssueRetag = definition.OnIssueRetag;
            resource.OnApplicationUpdate = definition.OnApplicationUpdate;
            resource.SupportsOnGrab = definition.SupportsOnGrab;
            resource.SupportsOnReleaseImport = definition.SupportsOnReleaseImport;
            resource.SupportsOnUpgrade = definition.SupportsOnUpgrade;
            resource.SupportsOnRename = definition.SupportsOnRename;
            resource.SupportsOnSeriesAdded = definition.SupportsOnSeriesAdded;
            resource.SupportsOnSeriesDelete = definition.SupportsOnSeriesDelete;
            resource.SupportsOnIssueDelete = definition.SupportsOnIssueDelete;
            resource.SupportsOnComicFileDelete = definition.SupportsOnComicFileDelete;
            resource.SupportsOnComicFileDeleteForUpgrade = definition.SupportsOnComicFileDeleteForUpgrade;
            resource.SupportsOnHealthIssue = definition.SupportsOnHealthIssue;
            resource.IncludeHealthWarnings = definition.IncludeHealthWarnings;
            resource.SupportsOnDownloadFailure = definition.SupportsOnDownloadFailure;
            resource.SupportsOnImportFailure = definition.SupportsOnImportFailure;
            resource.SupportsOnIssueRetag = definition.SupportsOnIssueRetag;
            resource.SupportsOnApplicationUpdate = definition.SupportsOnApplicationUpdate;

            return resource;
        }

        public override NotificationDefinition ToModel(NotificationResource resource)
        {
            if (resource == null)
            {
                return default(NotificationDefinition);
            }

            var definition = base.ToModel(resource);

            definition.OnGrab = resource.OnGrab;
            definition.OnReleaseImport = resource.OnReleaseImport;
            definition.OnUpgrade = resource.OnUpgrade;
            definition.OnRename = resource.OnRename;
            definition.OnSeriesAdded = resource.OnSeriesAdded;
            definition.OnSeriesDelete = resource.OnSeriesDelete;
            definition.OnIssueDelete = resource.OnIssueDelete;
            definition.OnComicFileDelete = resource.OnComicFileDelete;
            definition.OnComicFileDeleteForUpgrade = resource.OnComicFileDeleteForUpgrade;
            definition.OnHealthIssue = resource.OnHealthIssue;
            definition.OnDownloadFailure = resource.OnDownloadFailure;
            definition.OnImportFailure = resource.OnImportFailure;
            definition.OnIssueRetag = resource.OnIssueRetag;
            definition.OnApplicationUpdate = resource.OnApplicationUpdate;
            definition.SupportsOnGrab = resource.SupportsOnGrab;
            definition.SupportsOnReleaseImport = resource.SupportsOnReleaseImport;
            definition.SupportsOnUpgrade = resource.SupportsOnUpgrade;
            definition.SupportsOnRename = resource.SupportsOnRename;
            definition.SupportsOnSeriesAdded = resource.SupportsOnSeriesAdded;
            definition.SupportsOnSeriesDelete = resource.SupportsOnSeriesDelete;
            definition.SupportsOnIssueDelete = resource.SupportsOnIssueDelete;
            definition.SupportsOnComicFileDelete = resource.SupportsOnComicFileDelete;
            definition.SupportsOnComicFileDeleteForUpgrade = resource.SupportsOnComicFileDeleteForUpgrade;
            definition.SupportsOnHealthIssue = resource.SupportsOnHealthIssue;
            definition.IncludeHealthWarnings = resource.IncludeHealthWarnings;
            definition.SupportsOnDownloadFailure = resource.SupportsOnDownloadFailure;
            definition.SupportsOnImportFailure = resource.SupportsOnImportFailure;
            definition.SupportsOnIssueRetag = resource.SupportsOnIssueRetag;
            definition.SupportsOnApplicationUpdate = resource.SupportsOnApplicationUpdate;

            return definition;
        }
    }
}
