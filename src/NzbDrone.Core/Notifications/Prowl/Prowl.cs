using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Notifications.Prowl
{
    public class Prowl : NotificationBase<ProwlSettings>
    {
        private readonly IProwlProxy _prowlProxy;

        public Prowl(IProwlProxy prowlProxy)
        {
            _prowlProxy = prowlProxy;
        }

        public override string Link => "https://www.prowlapp.com/";
        public override string Name => "Prowl";

        public override void OnGrab(GrabMessage message)
        {
            _prowlProxy.SendNotification(ISSUE_GRABBED_TITLE, message.Message, Settings);
        }

        public override void OnReleaseImport(IssueDownloadMessage message)
        {
            _prowlProxy.SendNotification(ISSUE_DOWNLOADED_TITLE, message.Message, Settings);
        }

        public override void OnSeriesAdded(Series series)
        {
            _prowlProxy.SendNotification(AUTHOR_ADDED_TITLE, series.Name, Settings);
        }

        public override void OnSeriesDelete(SeriesDeleteMessage deleteMessage)
        {
            _prowlProxy.SendNotification(AUTHOR_DELETED_TITLE, deleteMessage.Message, Settings);
        }

        public override void OnIssueDelete(IssueDeleteMessage deleteMessage)
        {
            _prowlProxy.SendNotification(ISSUE_DELETED_TITLE, deleteMessage.Message, Settings);
        }

        public override void OnComicFileDelete(ComicFileDeleteMessage deleteMessage)
        {
            _prowlProxy.SendNotification(ISSUE_FILE_DELETED_TITLE, deleteMessage.Message, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _prowlProxy.SendNotification(HEALTH_ISSUE_TITLE, healthCheck.Message, Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _prowlProxy.SendNotification(APPLICATION_UPDATE_TITLE, updateMessage.Message, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_prowlProxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
