using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Http;
using NzbDrone.Core.MetadataSource.Metron.Resources;

namespace NzbDrone.Core.MetadataSource.Metron
{
    public interface IMetronApiClient
    {
        List<MetronSeriesListItem> SearchSeries(string title);
        MetronSeriesDetail GetSeriesDetail(int id);
        List<MetronIssueListItem> GetIssuesBySeries(int seriesId);
        MetronIssueDetail GetIssueDetail(int id);
        MetronPublisherDetail GetPublisherDetail(int id);
    }

    public class MetronApiClient : IMetronApiClient
    {
        private const string BaseUrl = "https://metron.cloud/api/";
        private readonly ICachedHttpResponseService _cachedHttpClient;
        private readonly IConfigService _configService;
        private readonly MetronRateLimiter _rateLimiter;
        private readonly Logger _logger;

        public MetronApiClient(ICachedHttpResponseService cachedHttpClient,
                               IConfigService configService,
                               Logger logger)
        {
            _cachedHttpClient = cachedHttpClient;
            _configService = configService;
            _rateLimiter = new MetronRateLimiter();
            _logger = logger;
        }

        private HttpRequest BuildRequest(string endpoint)
        {
            var username = _configService.MetronUsername;
            var password = _configService.MetronPassword;
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

            return new HttpRequestBuilder(BaseUrl + "{endpoint}")
                .SetSegment("endpoint", endpoint)
                .SetHeader("Authorization", $"Basic {credentials}")
                .SetHeader("Accept", "application/json")
                .Build();
        }

        public List<MetronSeriesListItem> SearchSeries(string title)
        {
            _rateLimiter.WaitForToken();

            var request = BuildRequest("series/");
            request.Url = request.Url.AddQueryParam("name", title);

            var response = _cachedHttpClient.Get<MetronPagedResponse<MetronSeriesListItem>>(request, false, TimeSpan.FromHours(1));

            var results = response.Resource?.Results ?? new List<MetronSeriesListItem>();

            return FetchAllPages(response.Resource, results, TimeSpan.FromHours(1));
        }

        public MetronSeriesDetail GetSeriesDetail(int id)
        {
            _rateLimiter.WaitForToken();

            var request = BuildRequest($"series/{id}/");

            var response = _cachedHttpClient.Get<MetronSeriesDetail>(request, true, TimeSpan.FromHours(24));

            return response.Resource;
        }

        public List<MetronIssueListItem> GetIssuesBySeries(int seriesId)
        {
            _rateLimiter.WaitForToken();

            var request = BuildRequest("issue/");
            request.Url = request.Url.AddQueryParam("series_id", seriesId.ToString());

            var response = _cachedHttpClient.Get<MetronPagedResponse<MetronIssueListItem>>(request, true, TimeSpan.FromHours(12));

            var results = response.Resource?.Results ?? new List<MetronIssueListItem>();

            return FetchAllPages(response.Resource, results, TimeSpan.FromHours(12));
        }

        public MetronIssueDetail GetIssueDetail(int id)
        {
            _rateLimiter.WaitForToken();

            var request = BuildRequest($"issue/{id}/");

            var response = _cachedHttpClient.Get<MetronIssueDetail>(request, true, TimeSpan.FromHours(12));

            return response.Resource;
        }

        private List<T> FetchAllPages<T>(MetronPagedResponse<T> firstPage, List<T> results, TimeSpan cacheDuration)
        {
            var nextUrl = firstPage?.Next;

            while (!string.IsNullOrEmpty(nextUrl))
            {
                _rateLimiter.WaitForToken();

                var request = new HttpRequest(nextUrl);
                var username = _configService.MetronUsername;
                var password = _configService.MetronPassword;
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                request.Headers.Set("Authorization", $"Basic {credentials}");
                request.Headers.Set("Accept", "application/json");

                var response = _cachedHttpClient.Get<MetronPagedResponse<T>>(request, true, cacheDuration);

                if (response.Resource?.Results != null)
                {
                    results.AddRange(response.Resource.Results);
                }

                nextUrl = response.Resource?.Next;
            }

            return results;
        }

        public MetronPublisherDetail GetPublisherDetail(int id)
        {
            _rateLimiter.WaitForToken();

            var request = BuildRequest($"publisher/{id}/");

            var response = _cachedHttpClient.Get<MetronPublisherDetail>(request, true, TimeSpan.FromHours(48));

            return response.Resource;
        }
    }
}
