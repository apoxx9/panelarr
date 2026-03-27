using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NzbDrone.Common.Http;
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
        private readonly MetronRateLimiter _rateLimiter;
        private readonly Logger _logger;
        private readonly IHttpRequestBuilderFactory _requestBuilder;

        public MetronApiClient(ICachedHttpResponseService cachedHttpClient,
                               MetronSettings settings,
                               Logger logger)
        {
            _cachedHttpClient = cachedHttpClient;
            _rateLimiter = new MetronRateLimiter();
            _logger = logger;

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.Username}:{settings.Password}"));

            _requestBuilder = new HttpRequestBuilder(BaseUrl + "{endpoint}")
                .SetHeader("Authorization", $"Basic {credentials}")
                .SetHeader("Accept", "application/json")
                .KeepAlive()
                .CreateFactory();
        }

        public List<MetronSeriesListItem> SearchSeries(string title)
        {
            _rateLimiter.WaitForToken();

            var request = _requestBuilder.Create()
                .SetSegment("endpoint", "series/")
                .AddQueryParam("name", title)
                .Build();

            var response = _cachedHttpClient.Get<MetronPagedResponse<MetronSeriesListItem>>(request, false, TimeSpan.FromHours(1));

            return response.Resource?.Results ?? new List<MetronSeriesListItem>();
        }

        public MetronSeriesDetail GetSeriesDetail(int id)
        {
            _rateLimiter.WaitForToken();

            var request = _requestBuilder.Create()
                .SetSegment("endpoint", $"series/{id}/")
                .Build();

            var response = _cachedHttpClient.Get<MetronSeriesDetail>(request, true, TimeSpan.FromHours(24));

            return response.Resource;
        }

        public List<MetronIssueListItem> GetIssuesBySeries(int seriesId)
        {
            _rateLimiter.WaitForToken();

            var request = _requestBuilder.Create()
                .SetSegment("endpoint", "issue/")
                .AddQueryParam("series_id", seriesId.ToString())
                .Build();

            var response = _cachedHttpClient.Get<MetronPagedResponse<MetronIssueListItem>>(request, true, TimeSpan.FromHours(12));

            return response.Resource?.Results ?? new List<MetronIssueListItem>();
        }

        public MetronIssueDetail GetIssueDetail(int id)
        {
            _rateLimiter.WaitForToken();

            var request = _requestBuilder.Create()
                .SetSegment("endpoint", $"issue/{id}/")
                .Build();

            var response = _cachedHttpClient.Get<MetronIssueDetail>(request, true, TimeSpan.FromHours(12));

            return response.Resource;
        }

        public MetronPublisherDetail GetPublisherDetail(int id)
        {
            _rateLimiter.WaitForToken();

            var request = _requestBuilder.Create()
                .SetSegment("endpoint", $"publisher/{id}/")
                .Build();

            var response = _cachedHttpClient.Get<MetronPublisherDetail>(request, true, TimeSpan.FromHours(48));

            return response.Resource;
        }
    }
}
