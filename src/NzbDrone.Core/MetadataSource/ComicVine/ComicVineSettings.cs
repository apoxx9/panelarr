namespace NzbDrone.Core.MetadataSource.ComicVine
{
    public class ComicVineSettings
    {
        public const string DefaultBaseUrl = "https://comicvine.gamespot.com/api";

        public string ApiKey { get; set; }
        public string BaseUrl { get; set; } = DefaultBaseUrl;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    }
}
