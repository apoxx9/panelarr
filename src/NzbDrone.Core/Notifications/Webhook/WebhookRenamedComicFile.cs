using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRenamedComicFile : WebhookComicFile
    {
        public WebhookRenamedComicFile(RenamedComicFile renamedMovie)
            : base(renamedMovie.ComicFile)
        {
            PreviousPath = renamedMovie.PreviousPath;
        }

        public string PreviousPath { get; set; }
    }
}
