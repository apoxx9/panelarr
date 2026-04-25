using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.FileList
{
    public class FileListRequestGenerator : IIndexerRequestGenerator
    {
        public FileListSettings Settings { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest("latest-torrents", Settings.Categories, ""));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(IssueSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var seriesQuery = searchCriteria.SeriesQuery.Replace("+", " ").Trim();
            var issueQuery = searchCriteria.IssueQuery.Replace("+", " ").Trim();

            pageableRequests.Add(GetRequest("search-torrents", Settings.Categories, string.Format("&type=name&query={0}+{1}", Uri.EscapeDataString(seriesQuery), Uri.EscapeDataString(issueQuery))));
            pageableRequests.Add(GetRequest("search-torrents", Settings.Categories, string.Format("&type=name&query={0}+{1}", Uri.EscapeDataString(issueQuery), Uri.EscapeDataString(seriesQuery))));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(SeriesSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var seriesQuery = searchCriteria.SeriesQuery.Replace("+", " ").Trim();

            pageableRequests.Add(GetRequest("search-torrents", Settings.Categories, string.Format("&type=name&query={0}", Uri.EscapeDataString(seriesQuery))));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchType, IEnumerable<int> categories, string parameters)
        {
            var categoriesQuery = string.Join(",", categories.Distinct());

            var baseUrl = string.Format("{0}/api.php?action={1}&category={2}{3}", Settings.BaseUrl.TrimEnd('/'), searchType, categoriesQuery, parameters);

            var request = new IndexerRequest(baseUrl, HttpAccept.Json);
            request.HttpRequest.Credentials = new BasicNetworkCredential(Settings.Username.Trim(), Settings.Passkey.Trim());

            yield return request;
        }
    }
}
