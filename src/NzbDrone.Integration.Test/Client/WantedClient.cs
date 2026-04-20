using System.Collections.Generic;
using Panelarr.Api.V1.Issues;
using Panelarr.Http;
using RestSharp;

namespace NzbDrone.Integration.Test.Client
{
    public class WantedClient : ClientBase<IssueResource>
    {
        public WantedClient(IRestClient restClient, string apiKey, string resource)
            : base(restClient, apiKey, resource)
        {
        }

        public PagingResource<IssueResource> GetPagedIncludeSeries(int pageNumber, int pageSize, string sortKey, string sortDir, string filterKey = null, string filterValue = null, bool includeSeries = true)
        {
            var request = BuildRequest();
            request.AddParameter("page", pageNumber);
            request.AddParameter("pageSize", pageSize);
            request.AddParameter("sortKey", sortKey);
            request.AddParameter("sortDir", sortDir);

            if (filterKey != null && filterValue != null)
            {
                request.AddParameter("filterKey", filterKey);
                request.AddParameter("filterValue", filterValue);
            }

            request.AddParameter("includeSeries", includeSeries);

            return Get<PagingResource<IssueResource>>(request);
        }

        public List<IssueResource> GetIssuesInSeries(int seriesId)
        {
            var request = BuildRequest("?seriesId=" + seriesId.ToString());
            return Get<List<IssueResource>>(request);
        }
    }
}
