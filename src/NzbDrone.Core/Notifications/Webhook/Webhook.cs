using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class Webhook : WebhookBase<WebhookSettings>
    {
        private readonly IWebhookProxy _proxy;

        public Webhook(IWebhookProxy proxy, IConfigFileProvider configFileProvider)
            : base(configFileProvider)
        {
            _proxy = proxy;
        }

        public override string Link => "https://wiki.servarr.com/panelarr/settings#connections";

        public override void OnGrab(GrabMessage message)
        {
            _proxy.SendWebhook(BuildOnGrabPayload(message), Settings);
        }

        public override void OnReleaseImport(IssueDownloadMessage message)
        {
            _proxy.SendWebhook(BuildOnReleaseImportPayload(message), Settings);
        }

        public override void OnRename(Series series, List<RenamedComicFile> renamedFiles)
        {
            _proxy.SendWebhook(BuildOnRenamePayload(series, renamedFiles), Settings);
        }

        public override void OnSeriesAdded(Series series)
        {
            _proxy.SendWebhook(BuildOnSeriesAdded(series), Settings);
        }

        public override void OnSeriesDelete(SeriesDeleteMessage deleteMessage)
        {
            _proxy.SendWebhook(BuildOnSeriesDelete(deleteMessage), Settings);
        }

        public override void OnIssueDelete(IssueDeleteMessage deleteMessage)
        {
            _proxy.SendWebhook(BuildOnIssueDelete(deleteMessage), Settings);
        }

        public override void OnComicFileDelete(ComicFileDeleteMessage deleteMessage)
        {
            _proxy.SendWebhook(BuildOnComicFileDelete(deleteMessage), Settings);
        }

        public override void OnIssueRetag(IssueRetagMessage message)
        {
            _proxy.SendWebhook(BuildOnIssueRetagPayload(message), Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _proxy.SendWebhook(BuildHealthPayload(healthCheck), Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _proxy.SendWebhook(BuildApplicationUpdatePayload(updateMessage), Settings);
        }

        public override string Name => "Webhook";

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(SendWebhookTest());

            return new ValidationResult(failures);
        }

        private ValidationFailure SendWebhookTest()
        {
            try
            {
                _proxy.SendWebhook(BuildTestPayload(), Settings);
            }
            catch (WebhookException ex)
            {
                return new NzbDroneValidationFailure("Url", ex.Message);
            }

            return null;
        }
    }
}
