using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public abstract class NotificationBase<TSettings> : INotification
        where TSettings : IProviderConfig, new()
    {
        protected const string ISSUE_GRABBED_TITLE = "Issue Grabbed";
        protected const string ISSUE_DOWNLOADED_TITLE = "Issue Downloaded";
        protected const string AUTHOR_ADDED_TITLE = "Series Added";
        protected const string AUTHOR_DELETED_TITLE = "Series Deleted";
        protected const string ISSUE_DELETED_TITLE = "Issue Deleted";
        protected const string ISSUE_FILE_DELETED_TITLE = "Issue File Deleted";
        protected const string HEALTH_ISSUE_TITLE = "Health Check Failure";
        protected const string DOWNLOAD_FAILURE_TITLE = "Download Failed";
        protected const string IMPORT_FAILURE_TITLE = "Import Failed";
        protected const string ISSUE_RETAGGED_TITLE = "Issue File Tags Updated";
        protected const string APPLICATION_UPDATE_TITLE = "Application Updated";

        protected const string ISSUE_GRABBED_TITLE_BRANDED = "Panelarr - " + ISSUE_GRABBED_TITLE;
        protected const string ISSUE_DOWNLOADED_TITLE_BRANDED = "Panelarr - " + ISSUE_DOWNLOADED_TITLE;
        protected const string AUTHOR_ADDED_TITLE_BRANDED = "Panelarr - " + AUTHOR_ADDED_TITLE;
        protected const string AUTHOR_DELETED_TITlE_BRANDED = "Panelarr - " + AUTHOR_DELETED_TITLE;
        protected const string ISSUE_DELETED_TITLE_BRANDED = "Panelarr - " + ISSUE_DELETED_TITLE;
        protected const string ISSUE_FILE_DELETED_TITLE_BRANDED = "Panelarr - " + ISSUE_FILE_DELETED_TITLE;
        protected const string HEALTH_ISSUE_TITLE_BRANDED = "Panelarr - " + HEALTH_ISSUE_TITLE;
        protected const string DOWNLOAD_FAILURE_TITLE_BRANDED = "Panelarr - " + DOWNLOAD_FAILURE_TITLE;
        protected const string IMPORT_FAILURE_TITLE_BRANDED = "Panelarr - " + IMPORT_FAILURE_TITLE;
        protected const string ISSUE_RETAGGED_TITLE_BRANDED = "Panelarr - " + ISSUE_RETAGGED_TITLE;
        protected const string APPLICATION_UPDATE_TITLE_BRANDED = "Panelarr - " + APPLICATION_UPDATE_TITLE;

        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }
        public abstract ValidationResult Test();

        public abstract string Link { get; }

        public virtual void OnGrab(GrabMessage grabMessage)
        {
        }

        public virtual void OnReleaseImport(IssueDownloadMessage message)
        {
        }

        public virtual void OnRename(Series series, List<RenamedComicFile> renamedFiles)
        {
        }

        public virtual void OnSeriesAdded(Series series)
        {
        }

        public virtual void OnSeriesDelete(SeriesDeleteMessage deleteMessage)
        {
        }

        public virtual void OnIssueDelete(IssueDeleteMessage deleteMessage)
        {
        }

        public virtual void OnComicFileDelete(ComicFileDeleteMessage deleteMessage)
        {
        }

        public virtual void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
        }

        public virtual void OnDownloadFailure(DownloadFailedMessage message)
        {
        }

        public virtual void OnImportFailure(IssueDownloadMessage message)
        {
        }

        public virtual void OnIssueRetag(IssueRetagMessage message)
        {
        }

        public virtual void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
        }

        public virtual void ProcessQueue()
        {
        }

        public bool SupportsOnGrab => HasConcreteImplementation("OnGrab");
        public bool SupportsOnRename => HasConcreteImplementation("OnRename");
        public bool SupportsOnSeriesAdded => HasConcreteImplementation("OnSeriesAdded");
        public bool SupportsOnSeriesDelete => HasConcreteImplementation("OnSeriesDelete");
        public bool SupportsOnIssueDelete => HasConcreteImplementation("OnIssueDelete");
        public bool SupportsOnComicFileDelete => HasConcreteImplementation("OnComicFileDelete");
        public bool SupportsOnComicFileDeleteForUpgrade => SupportsOnComicFileDelete;
        public bool SupportsOnReleaseImport => HasConcreteImplementation("OnReleaseImport");
        public bool SupportsOnUpgrade => SupportsOnReleaseImport;
        public bool SupportsOnHealthIssue => HasConcreteImplementation("OnHealthIssue");
        public bool SupportsOnDownloadFailure => HasConcreteImplementation("OnDownloadFailure");
        public bool SupportsOnImportFailure => HasConcreteImplementation("OnImportFailure");
        public bool SupportsOnIssueRetag => HasConcreteImplementation("OnIssueRetag");
        public bool SupportsOnApplicationUpdate => HasConcreteImplementation("OnApplicationUpdate");

        protected TSettings Settings => (TSettings)Definition.Settings;

        public override string ToString()
        {
            return GetType().Name;
        }

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }

        private bool HasConcreteImplementation(string methodName)
        {
            var method = GetType().GetMethod(methodName);

            if (method == null)
            {
                throw new MissingMethodException(GetType().Name, Name);
            }

            return !method.DeclaringType.IsAbstract;
        }
    }
}
