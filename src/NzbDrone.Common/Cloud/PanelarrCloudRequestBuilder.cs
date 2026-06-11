using NzbDrone.Common.Http;

namespace NzbDrone.Common.Cloud
{
    public interface IPanelarrCloudRequestBuilder
    {
        IHttpRequestBuilderFactory Metadata { get; }
    }

    public class PanelarrCloudRequestBuilder : IPanelarrCloudRequestBuilder
    {
        public PanelarrCloudRequestBuilder()
        {
            // Panelarr uses ComicVine/Metron directly for metadata.
            // This endpoint is reserved for a future Panelarr metadata proxy;
            // updates and health pings use GitHub instead.
            Metadata = new HttpRequestBuilder("https://panelarr.servarr.com/v1/metadata/{route}")
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Metadata { get; }
    }
}
