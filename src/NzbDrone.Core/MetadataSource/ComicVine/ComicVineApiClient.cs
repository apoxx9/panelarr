using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Http;
using NzbDrone.Core.MetadataSource.ComicVine.Resources;

namespace NzbDrone.Core.MetadataSource.ComicVine
{
    public interface IComicVineApiClient
    {
        List<ComicVineVolumeSummary> SearchSeries(string query);
        List<ComicVineVolumeSummary> SearchVolumes(string query, int? year = null);
        ComicVineVolumeDetail GetVolume(int id);
        List<ComicVineIssueSummary> GetIssues(int volumeId);
        ComicVineIssueDetail GetIssue(int id);
        ComicVinePublisherDetail GetPublisher(int id);
        List<ComicVineStoryArcSummary> SearchStoryArcs(string query);
        ComicVineStoryArcSummary GetStoryArc(int id);
        List<ComicVineArcIssue> GetStoryArcIssues(int storyArcId);
    }

    public class ComicVineApiClient : IComicVineApiClient
    {
        private const string BaseUrl = "https://comicvine.gamespot.com/api";
        private const string UserAgent = "Panelarr/1.0";
        private readonly ICachedHttpResponseService _cachedHttpClient;
        private readonly IConfigService _configService;
        private readonly ComicVineRateLimiter _rateLimiter;
        private readonly Logger _logger;

        public ComicVineApiClient(ICachedHttpResponseService cachedHttpClient,
                                  IConfigService configService,
                                  Logger logger)
        {
            _cachedHttpClient = cachedHttpClient;
            _configService = configService;
            _rateLimiter = new ComicVineRateLimiter(logger);
            _logger = logger;
        }

        private string ApiKey => _configService.ComicVineApiKey;

        private HttpRequest BuildRequest(string endpoint)
        {
            return new HttpRequestBuilder(BaseUrl + "/{endpoint}/")
                .SetSegment("endpoint", endpoint)
                .AddQueryParam("api_key", ApiKey)
                .AddQueryParam("format", "json")
                .SetHeader("User-Agent", UserAgent)
                .Build();
        }

        public List<ComicVineVolumeSummary> SearchSeries(string query)
        {
            _rateLimiter.WaitForToken();

            var request = BuildRequest("search");
            request.Url = request.Url
                .AddQueryParam("resources", "volume")
                .AddQueryParam("query", query)
                .AddQueryParam("limit", "25");

            var response = _cachedHttpClient.Get<ComicVineResponse<List<ComicVineVolumeSummary>>>(request, false, TimeSpan.FromHours(1));

            return response.Resource?.Results ?? new List<ComicVineVolumeSummary>();
        }

        public List<ComicVineVolumeSummary> SearchVolumes(string query, int? year = null)
        {
            _rateLimiter.WaitForToken();

            var filter = year.HasValue
                ? $"name:{query},start_year:{year.Value}"
                : $"name:{query}";

            var request = BuildRequest("volumes");
            request.Url = request.Url
                .AddQueryParam("filter", filter)
                .AddQueryParam("field_list", "id,name,start_year,publisher,image,count_of_issues,deck,description");

            var response = _cachedHttpClient.Get<ComicVineResponse<List<ComicVineVolumeSummary>>>(request, false, TimeSpan.FromHours(1));

            return response.Resource?.Results ?? new List<ComicVineVolumeSummary>();
        }

        public ComicVineVolumeDetail GetVolume(int id)
        {
            _rateLimiter.WaitForToken();

            var request = BuildRequest($"volume/4050-{id}");
            request.Url = request.Url
                .AddQueryParam("field_list", "id,name,start_year,publisher,image,description,issues,count_of_issues");

            var response = _cachedHttpClient.Get<ComicVineResponse<ComicVineVolumeDetail>>(request, true, TimeSpan.FromHours(24));

            return response.Resource?.Results;
        }

        public List<ComicVineIssueSummary> GetIssues(int volumeId)
        {
            var allIssues = new List<ComicVineIssueSummary>();
            var offset = 0;
            const int limit = 100;

            while (true)
            {
                _rateLimiter.WaitForToken();

                var request = BuildRequest("issues");
                request.Url = request.Url
                    .AddQueryParam("filter", $"volume:{volumeId}")
                    .AddQueryParam("field_list", "id,name,issue_number,cover_date,image")
                    .AddQueryParam("limit", limit.ToString())
                    .AddQueryParam("offset", offset.ToString());

                var response = _cachedHttpClient.Get<ComicVineResponse<List<ComicVineIssueSummary>>>(request, true, TimeSpan.FromHours(12));

                var results = response.Resource?.Results ?? new List<ComicVineIssueSummary>();
                allIssues.AddRange(results);

                var total = response.Resource?.NumberOfTotalResults ?? 0;

                if (allIssues.Count >= total || results.Count == 0)
                {
                    break;
                }

                offset += limit;
                _logger.Debug("Fetching next page of issues for volume {0} (offset: {1}, total: {2})", volumeId, offset, total);
            }

            return allIssues;
        }

        public List<ComicVineStoryArcSummary> SearchStoryArcs(string query)
        {
            _rateLimiter.WaitForToken();

            var request = BuildRequest("story_arcs");
            request.Url = request.Url
                .AddQueryParam("filter", $"name:{query}")
                .AddQueryParam("field_list", "id,name,deck,publisher,count_of_issue_appearances")
                .AddQueryParam("limit", "25");

            var response = _cachedHttpClient.Get<ComicVineResponse<List<ComicVineStoryArcSummary>>>(request, false, TimeSpan.FromHours(1));

            return response.Resource?.Results ?? new List<ComicVineStoryArcSummary>();
        }

        public ComicVineStoryArcSummary GetStoryArc(int id)
        {
            _rateLimiter.WaitForToken();

            var request = BuildRequest($"story_arc/4045-{id}");
            request.Url = request.Url
                .AddQueryParam("field_list", "id,name,deck,publisher,count_of_issue_appearances,issues");

            var response = _cachedHttpClient.Get<ComicVineResponse<ComicVineStoryArcSummary>>(request, true, TimeSpan.FromHours(24));

            return response.Resource?.Results;
        }

        public List<ComicVineArcIssue> GetStoryArcIssues(int storyArcId)
        {
            // /issues does NOT support a story_arc filter (it silently ignores
            // unknown filter fields and returns everything — found the hard
            // way). The arc detail's issues array is the membership source;
            // it only carries {id, name}, so hydrate numbers/dates/volumes in
            // id-batches of 100 via filter=id:a|b|c.
            var arc = GetStoryArc(storyArcId);
            var issueIds = arc?.Issues?.Select(i => i.Id).ToList() ?? new List<int>();

            var allIssues = new List<ComicVineArcIssue>();

            foreach (var batch in issueIds.Chunk(100))
            {
                _rateLimiter.WaitForToken();

                var request = BuildRequest("issues");
                request.Url = request.Url
                    .AddQueryParam("filter", $"id:{string.Join("|", batch)}")
                    .AddQueryParam("field_list", "id,name,issue_number,cover_date,volume")
                    .AddQueryParam("limit", "100");

                var response = _cachedHttpClient.Get<ComicVineResponse<List<ComicVineArcIssue>>>(request, true, TimeSpan.FromHours(12));

                allIssues.AddRange(response.Resource?.Results ?? new List<ComicVineArcIssue>());

                _logger.Debug("Hydrated {0}/{1} arc issues for story arc {2}", allIssues.Count, issueIds.Count, storyArcId);
            }

            return allIssues;
        }

        public ComicVineIssueDetail GetIssue(int id)
        {
            _rateLimiter.WaitForToken();

            var request = BuildRequest($"issue/4000-{id}");
            request.Url = request.Url
                .AddQueryParam("field_list", "id,name,issue_number,cover_date,image,description,volume");

            var response = _cachedHttpClient.Get<ComicVineResponse<ComicVineIssueDetail>>(request, true, TimeSpan.FromHours(12));

            return response.Resource?.Results;
        }

        public ComicVinePublisherDetail GetPublisher(int id)
        {
            _rateLimiter.WaitForToken();

            var request = BuildRequest($"publisher/4010-{id}");
            request.Url = request.Url
                .AddQueryParam("field_list", "id,name,description,image");

            var response = _cachedHttpClient.Get<ComicVineResponse<ComicVinePublisherDetail>>(request, true, TimeSpan.FromHours(24));

            return response.Resource?.Results;
        }
    }
}
