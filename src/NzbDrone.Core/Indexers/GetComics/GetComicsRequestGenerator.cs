using System.Collections.Generic;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.GetComics
{
    public class GetComicsRequestGenerator : IIndexerRequestGenerator
    {
        public GetComicsSettings Settings { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(null));

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(IssueSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            // Search by series name + issue title for best results
            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SeriesQuery}+{searchCriteria.IssueQuery}"));

            // Fall back to just series name
            pageableRequests.AddTier(GetPagedRequests(searchCriteria.SeriesQuery));

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(SeriesSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria.SeriesQuery));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string query)
        {
            var baseUrl = Settings.BaseUrl.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(query))
            {
                // For recent/RSS, just fetch the homepage which has the latest posts
                yield return new IndexerRequest($"{baseUrl}/", HttpAccept.Html);
            }
            else
            {
                yield return new IndexerRequest($"{baseUrl}/?s={query}", HttpAccept.Html);
            }
        }
    }
}
