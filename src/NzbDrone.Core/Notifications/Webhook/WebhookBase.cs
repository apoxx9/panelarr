using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Books;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications.Webhook
{
    public abstract class WebhookBase<TSettings> : NotificationBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        private readonly IConfigFileProvider _configFileProvider;

        protected WebhookBase(IConfigFileProvider configFileProvider)
            : base()
        {
            _configFileProvider = configFileProvider;
        }

        public WebhookGrabPayload BuildOnGrabPayload(GrabMessage message)
        {
            var remoteBook = message.RemoteBook;
            var quality = message.Quality;

            return new WebhookGrabPayload
            {
                EventType = WebhookEventType.Grab,
                InstanceName = _configFileProvider.InstanceName,
                Series = new WebhookSeries(message.Series),
                Books = remoteBook.Books.ConvertAll(x => new WebhookBook(x)),
                Release = new WebhookRelease(quality, remoteBook),
                DownloadClient = message.DownloadClientName,
                DownloadClientType = message.DownloadClientType,
                DownloadId = message.DownloadId
            };
        }

        public WebhookImportPayload BuildOnReleaseImportPayload(IssueDownloadMessage message)
        {
            var trackFiles = message.ComicFiles;

            var payload = new WebhookImportPayload
            {
                EventType = WebhookEventType.Download,
                InstanceName = _configFileProvider.InstanceName,
                Series = new WebhookSeries(message.Series),
                Issue = new WebhookBook(message.Issue),
                ComicFiles = trackFiles.ConvertAll(x => new WebhookBookFile(x)),
                IsUpgrade = message.OldFiles.Any(),
                DownloadClient = message.DownloadClientInfo?.Name,
                DownloadClientType = message.DownloadClientInfo?.Type,
                DownloadId = message.DownloadId
            };

            if (message.OldFiles.Any())
            {
                payload.DeletedFiles = message.OldFiles.ConvertAll(x => new WebhookBookFile(x));
            }

            return payload;
        }

        public WebhookRenamePayload BuildOnRenamePayload(Series author, List<RenamedComicFile> renamedFiles)
        {
            return new WebhookRenamePayload
            {
                EventType = WebhookEventType.Rename,
                InstanceName = _configFileProvider.InstanceName,
                Series = new WebhookSeries(author),
                RenamedBookFiles = renamedFiles.ConvertAll(x => new WebhookRenamedBookFile(x))
            };
        }

        public WebhookRetagPayload BuildOnIssueRetagPayload(IssueRetagMessage message)
        {
            return new WebhookRetagPayload
            {
                EventType = WebhookEventType.Retag,
                InstanceName = _configFileProvider.InstanceName,
                Series = new WebhookSeries(message.Series),
                ComicFile = new WebhookBookFile(message.ComicFile)
            };
        }

        public WebhookBookDeletePayload BuildOnIssueDelete(IssueDeleteMessage deleteMessage)
        {
            return new WebhookBookDeletePayload
            {
                EventType = WebhookEventType.IssueDelete,
                InstanceName = _configFileProvider.InstanceName,
                Series = new WebhookSeries(deleteMessage.Issue.Series),
                Issue = new WebhookBook(deleteMessage.Issue),
                DeletedFiles = deleteMessage.DeletedFiles
            };
        }

        public WebhookBookFileDeletePayload BuildOnComicFileDelete(ComicFileDeleteMessage deleteMessage)
        {
            return new WebhookBookFileDeletePayload
            {
                EventType = WebhookEventType.ComicFileDelete,
                InstanceName = _configFileProvider.InstanceName,
                Series = new WebhookSeries(deleteMessage.Issue.Series),
                Issue = new WebhookBook(deleteMessage.Issue),
                ComicFile = new WebhookBookFile(deleteMessage.ComicFile)
            };
        }

        public WebhookSeriesAddedPayload BuildOnSeriesAdded(Series author)
        {
            return new WebhookSeriesAddedPayload
            {
                EventType = WebhookEventType.SeriesAdded,
                InstanceName = _configFileProvider.InstanceName,
                Series = new WebhookSeries(author)
            };
        }

        public WebhookSeriesDeletePayload BuildOnSeriesDelete(SeriesDeleteMessage deleteMessage)
        {
            return new WebhookSeriesDeletePayload
            {
                EventType = WebhookEventType.SeriesDelete,
                InstanceName = _configFileProvider.InstanceName,
                Series = new WebhookSeries(deleteMessage.Series),
                DeletedFiles = deleteMessage.DeletedFiles
            };
        }

        protected WebhookHealthPayload BuildHealthPayload(HealthCheck.HealthCheck healthCheck)
        {
            return new WebhookHealthPayload
            {
                EventType = WebhookEventType.Health,
                InstanceName = _configFileProvider.InstanceName,
                Level = healthCheck.Type,
                Message = healthCheck.Message,
                Type = healthCheck.Source.Name,
                WikiUrl = healthCheck.WikiUrl?.ToString()
            };
        }

        protected WebhookApplicationUpdatePayload BuildApplicationUpdatePayload(ApplicationUpdateMessage updateMessage)
        {
            return new WebhookApplicationUpdatePayload
            {
                EventType = WebhookEventType.ApplicationUpdate,
                InstanceName = _configFileProvider.InstanceName,
                Message = updateMessage.Message,
                PreviousVersion = updateMessage.PreviousVersion.ToString(),
                NewVersion = updateMessage.NewVersion.ToString()
            };
        }

        protected WebhookPayload BuildTestPayload()
        {
            return new WebhookGrabPayload
            {
                EventType = WebhookEventType.Test,
                InstanceName = _configFileProvider.InstanceName,
                Series = new WebhookSeries()
                {
                    Id = 1,
                    Name = "Test Name",
                    Path = "C:\\testpath",
                    GoodreadsId = "aaaaa-aaa-aaaa-aaaaaa"
                },
                Books = new List<WebhookBook>()
                    {
                            new WebhookBook()
                            {
                                Id = 123,
                                Title = "Test title"
                            }
                    }
            };
        }
    }
}
