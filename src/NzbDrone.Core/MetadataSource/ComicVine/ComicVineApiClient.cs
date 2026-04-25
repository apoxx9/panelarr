using System;
using System.Collections.Generic;
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
        List<ComicVineVolumeSummary> SearchVolumes(string query);
        ComicVineVolumeDetail GetVolume(int id);
        List<ComicVineIssueSummary> GetIssues(int volumeId);
        ComicVineIssueDetail GetIssue(int id);
        ComicVinePublisherDetail GetPublisher(int id);
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
            _rateLimiter = new ComicVineRateLimiter();
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

        public List<ComicVineVolumeSummary> SearchVolumes(string query)
        {
            _rateLimiter.WaitForToken();

            var request = BuildRequest("volumes");
            request.Url = request.Url
                .AddQueryParam("filter", $"name:{query}")
                .AddQueryParam("field_list", "id,name,start_year,publisher,image,count_of_issues");

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
            _rateLimiter.WaitForToken();

            var request = BuildRequest("issues");
            request.Url = request.Url
                .AddQueryParam("filter", $"volume:{volumeId}")
                .AddQueryParam("field_list", "id,name,issue_number,cover_date,image");

            var response = _cachedHttpClient.Get<ComicVineResponse<List<ComicVineIssueSummary>>>(request, true, TimeSpan.FromHours(12));

            return response.Resource?.Results ?? new List<ComicVineIssueSummary>();
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
