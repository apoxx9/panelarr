using Panelarr.Api.V1.Issues;
using Panelarr.Api.V1.Series;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Search
{
    public class SearchResource : RestResource
    {
        public string ForeignId { get; set; }
        public SeriesResource Series { get; set; }
        public IssueResource Issue { get; set; }
    }
}
