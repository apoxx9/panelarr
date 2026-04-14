using System.Collections.Generic;
using System.Text.RegularExpressions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.GetComics
{
    public class GetComicsRequestGenerator : IIndexerRequestGenerator
    {
        private static readonly Regex SpecialChars = new Regex(@"[&:?/\-]", RegexOptions.Compiled);

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

            var seriesName = CleanQuery(searchCriteria.Series?.Name ?? "");
            var issueNum = searchCriteria.IssueNumber > 0 ? ((int)searchCriteria.IssueNumber).ToString() : null;
            var year = searchCriteria.IssueYear > 0 ? searchCriteria.IssueYear.ToString() : null;

            // Mylar-style tiered search: most specific first
            if (!string.IsNullOrEmpty(issueNum) && !string.IsNullOrEmpty(year))
            {
                // Tier 1: "Series #N (Year)" in quotes
                pageableRequests.Add(GetPagedRequests($"\"{seriesName} #{issueNum} ({year})\""));

                // Tier 2: Series #N (Year) without quotes
                pageableRequests.AddTier(GetPagedRequests($"{seriesName} #{issueNum} ({year})"));
            }

            if (!string.IsNullOrEmpty(issueNum))
            {
                // Tier 3: Series #N
                pageableRequests.AddTier(GetPagedRequests($"{seriesName} #{issueNum}"));
            }

            // Tier 4: Just series name
            pageableRequests.AddTier(GetPagedRequests(seriesName));

            return pageableRequests;
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(SeriesSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var seriesName = CleanQuery(searchCriteria.Series?.Name ?? "");
            pageableRequests.Add(GetPagedRequests(seriesName));

            return pageableRequests;
        }

        private static string CleanQuery(string query)
        {
            // Strip special characters like Mylar does
            var cleaned = SpecialChars.Replace(query, " ");
            return Regex.Replace(cleaned, @"\s+", " ").Trim();
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string query)
        {
            var baseUrl = Settings.BaseUrl.TrimEnd('/');

            if (string.IsNullOrWhiteSpace(query))
            {
                yield return new IndexerRequest($"{baseUrl}/", HttpAccept.Html);
            }
            else
            {
                yield return new IndexerRequest($"{baseUrl}/?s={System.Uri.EscapeDataString(query)}", HttpAccept.Html);
            }
        }
    }
}
