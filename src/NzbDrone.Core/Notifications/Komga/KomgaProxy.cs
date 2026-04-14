using System;
using System.Net.Http;
using System.Text;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Notifications.Komga
{
    public interface IKomgaProxy
    {
        void TriggerLibraryScan(KomgaSettings settings);
    }

    public class KomgaProxy : IKomgaProxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public KomgaProxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public void TriggerLibraryScan(KomgaSettings settings)
        {
            var baseUrl = settings.BaseUrl.TrimEnd('/');
            var requestBuilder = new HttpRequestBuilder(baseUrl + "/api/v1/libraries/scan")
            {
                Method = HttpMethod.Post
            };

            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}"));
            requestBuilder.Headers["Authorization"] = $"Basic {credentials}";

            var request = requestBuilder.Build();

            try
            {
                var response = _httpClient.Post(request);
                _logger.Trace("Komga library scan triggered. Response: {0}", response.StatusCode);
            }
            catch (HttpException ex)
            {
                throw new KomgaException("Unable to trigger Komga library scan", ex);
            }
        }
    }
}
