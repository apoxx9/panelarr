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
            //TODO: Create Update Endpoint
            Services = new HttpRequestBuilder("https://panelarr.servarr.com/v1/")
                .CreateFactory();

            // TODO: Panelarr uses ComicVine/Metron for metadata, not IssueInfo.
            // This legacy endpoint should be replaced once a Panelarr metadata proxy is set up.
            Metadata = new HttpRequestBuilder("https://api.bookinfo.club/v1/{route}")
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Services { get; }

        public IHttpRequestBuilderFactory Metadata { get; }
    }
}
