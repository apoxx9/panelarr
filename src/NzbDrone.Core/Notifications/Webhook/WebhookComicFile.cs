using System;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class WebhookComicFile
    {
        public WebhookComicFile()
        {
        }

        public WebhookComicFile(ComicFile comicFile)
        {
            Id = comicFile.Id;
            Path = comicFile.Path;
            Quality = comicFile.Quality.Quality.Name;
            QualityVersion = comicFile.Quality.Revision.Version;
            ReleaseGroup = comicFile.ReleaseGroup;
            SceneName = comicFile.SceneName;
            Size = comicFile.Size;
            DateAdded = comicFile.DateAdded;
        }

        public int Id { get; set; }
        public string Path { get; set; }
        public string Quality { get; set; }
        public int QualityVersion { get; set; }
        public string ReleaseGroup { get; set; }
        public string SceneName { get; set; }
        public long Size { get; set; }
        public DateTime DateAdded { get; set; }
    }
}
