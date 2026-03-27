using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookRenamedBookFile : WebhookBookFile
    {
        public WebhookRenamedBookFile(RenamedComicFile renamedMovie)
            : base(renamedMovie.ComicFile)
        {
            PreviousPath = renamedMovie.PreviousPath;
        }

        public string PreviousPath { get; set; }
    }
}
