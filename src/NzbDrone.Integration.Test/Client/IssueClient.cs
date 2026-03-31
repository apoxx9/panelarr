using System.Collections.Generic;
using Panelarr.Api.V1.Issues;
using RestSharp;

namespace NzbDrone.Integration.Test.Client
{
    public class IssueClient : ClientBase<IssueResource>
    {
        public IssueClient(IRestClient restClient, string apiKey)
            : base(restClient, apiKey, "issue")
        {
        }

        public List<IssueResource> GetIssuesInSeries(int authorId)
        {
            var request = BuildRequest("?authorId=" + authorId.ToString());
            return Get<List<IssueResource>>(request);
        }
    }
}
