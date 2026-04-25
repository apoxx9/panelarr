using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Notifications.Mailgun
{
    public class MailGun : NotificationBase<MailgunSettings>
    {
        private readonly IMailgunProxy _proxy;
        private readonly Logger _logger;

        public MailGun(IMailgunProxy proxy, Logger logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public override string Name => "Mailgun";
        public override string Link => "https://mailgun.com";

        public override void OnGrab(GrabMessage grabMessage)
        {
            _proxy.SendNotification(ISSUE_GRABBED_TITLE, grabMessage.Message, Settings);
        }

        public override void OnReleaseImport(IssueDownloadMessage downloadMessage)
        {
            _proxy.SendNotification(ISSUE_DOWNLOADED_TITLE, downloadMessage.Message, Settings);
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

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheckMessage)
        {
            _proxy.SendNotification(HEALTH_ISSUE_TITLE, healthCheckMessage.Message, Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _proxy.SendNotification(APPLICATION_UPDATE_TITLE, updateMessage.Message, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            try
            {
                const string title = "Test Notification";
                const string body = "This is a test message from Panelarr, though Mailgun.";

                _proxy.SendNotification(title, body, Settings);
                _logger.Info("Successsfully sent email though Mailgun.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to send test message though Mailgun.");
                failures.Add(new ValidationFailure("", "Unable to send test message though Mailgun."));
            }

            return new ValidationResult(failures);
        }
    }
}
