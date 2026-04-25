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

            // Panelarr uses ComicVine/Metron directly for metadata.
            // This endpoint is reserved for a future Panelarr metadata proxy.
            Metadata = new HttpRequestBuilder("https://panelarr.servarr.com/v1/metadata/{route}")
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Services { get; }

        public IHttpRequestBuilderFactory Metadata { get; }
    }
}
