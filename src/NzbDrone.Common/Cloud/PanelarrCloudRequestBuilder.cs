using NzbDrone.Common.Http;

namespace NzbDrone.Common.Cloud
{
    public interface IPanelarrCloudRequestBuilder
    {
        IHttpRequestBuilderFactory Services { get; }
        IHttpRequestBuilderFactory Metadata { get; }
    }

    public class PanelarrCloudRequestBuilder : IPanelarrCloudRequestBuilder
    {
        public PanelarrCloudRequestBuilder()
        {
            Services = new HttpRequestBuilder("https://panelarr.servarr.com/v1/")
                .CreateFactory();

            // Legacy endpoint from Readarr — Panelarr uses ComicVine/Metron directly.
            // Kept as fallback until a dedicated Panelarr metadata proxy exists.
            Metadata = new HttpRequestBuilder("https://api.bookinfo.club/v1/{route}")
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Services { get; }

        public IHttpRequestBuilderFactory Metadata { get; }
    }
}
