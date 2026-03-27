using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Notifications.Join
{
    public class Join : NotificationBase<JoinSettings>
    {
        private readonly IJoinProxy _proxy;

        public Join(IJoinProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Name => "Join";

        public override string Link => "https://joaoapps.com/join/";

        public override void OnGrab(GrabMessage message)
        {
            _proxy.SendNotification(BOOK_GRABBED_TITLE_BRANDED, message.Message, Settings);
        }

        public override void OnReleaseImport(IssueDownloadMessage message)
        {
            _proxy.SendNotification(BOOK_DOWNLOADED_TITLE_BRANDED, message.Message, Settings);
        }

        public override void OnSeriesAdded(Series author)
        {
            _proxy.SendNotification(AUTHOR_ADDED_TITLE_BRANDED, author.Name, Settings);
        }

        public override void OnSeriesDelete(SeriesDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(AUTHOR_DELETED_TITlE_BRANDED, deleteMessage.Message, Settings);
        }

        public override void OnBookDelete(IssueDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(BOOK_DELETED_TITLE_BRANDED, deleteMessage.Message, Settings);
        }

        public override void OnBookFileDelete(ComicFileDeleteMessage deleteMessage)
        {
            _proxy.SendNotification(BOOK_FILE_DELETED_TITLE_BRANDED, deleteMessage.Message, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck message)
        {
            _proxy.SendNotification(HEALTH_ISSUE_TITLE_BRANDED, message.Message, Settings);
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
