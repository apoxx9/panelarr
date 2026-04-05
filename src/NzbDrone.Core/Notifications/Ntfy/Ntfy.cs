using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Notifications.Ntfy
{
    public class Ntfy : NotificationBase<NtfySettings>
    {
        private readonly INtfyProxy _proxy;

        public Ntfy(INtfyProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Name => "ntfy.sh";

        public override string Link => "https://ntfy.sh/";

        public override void OnGrab(GrabMessage grabMessage)
        {
            _proxy.SendNotification(ISSUE_GRABBED_TITLE_BRANDED, grabMessage.Message, Settings);
        }

        public override void OnReleaseImport(IssueDownloadMessage message)
        {
            _proxy.SendNotification(ISSUE_DOWNLOADED_TITLE_BRANDED, message.Message, Settings);
        }

        public override void OnSeriesAdded(Series series)
        {
            _proxy.SendNotification(AUTHOR_ADDED_TITLE, series.Name, Settings);
        }

        public override void OnSeriesDelete(SeriesDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(AUTHOR_DELETED_TITLE, deleteMessage.Message, Settings);
        }

        public override void OnIssueDelete(IssueDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(ISSUE_DELETED_TITLE, deleteMessage.Message, Settings);
        }

        public override void OnComicFileDelete(ComicFileDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(ISSUE_FILE_DELETED_TITLE, deleteMessage.Message, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _proxy.SendNotification(HEALTH_ISSUE_TITLE_BRANDED, healthCheck.Message, Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _proxy.SendNotification(APPLICATION_UPDATE_TITLE_BRANDED, updateMessage.Message, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
