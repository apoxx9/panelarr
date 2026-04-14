namespace NzbDrone.Core.Indexers.GetComics
{
    public class GetComicsDownloadLink
    {
        public string Url { get; set; }
        public GetComicsDownloadHost Host { get; set; }
        public string Label { get; set; }

        /// <summary>
        /// Whether this link goes through getcomics.org/dlds/ redirect (needs HTTP follow).
        /// </summary>
        public bool IsRedirect { get; set; }
    }

    public enum GetComicsDownloadHost
    {
        Unknown = 0,
        DataNodes = 1,
        Pixeldrain = 2,
        Vikingfile = 3,
        Fileq = 4,
        TeraBox = 5,
        Rootz = 6,
        Mega = 10,
        MediaFire = 11,
        GoogleDrive = 12,
    }
}
