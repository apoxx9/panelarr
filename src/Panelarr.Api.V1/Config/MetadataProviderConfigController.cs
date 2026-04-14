using System;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using Panelarr.Http;

namespace Panelarr.Api.V1.Config
{
    [V1ApiController("config/metadataprovider")]
    public class MetadataProviderConfigController : ConfigController<MetadataProviderConfigResource>
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public MetadataProviderConfigController(IConfigService configService,
                                                IHttpClient httpClient,
                                                Logger logger)
            : base(configService)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        protected override MetadataProviderConfigResource ToResource(IConfigService model)
        {
            return MetadataProviderConfigResourceMapper.ToResource(model);
        }

        [HttpGet("test")]
        public object TestMetronCredentials()
        {
            var username = _configService.MetronUsername;
            var password = _configService.MetronPassword;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return new { isValid = false, message = "Metron username and password must be configured" };
            }

            try
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

                var request = new HttpRequestBuilder("https://metron.cloud/api/series/")
                    .SetHeader("Authorization", $"Basic {credentials}")
                    .SetHeader("Accept", "application/json")
                    .Build();

                request.SuppressHttpError = true;

                var response = _httpClient.Get(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return new { isValid = true };
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return new { isValid = false, message = "Invalid username or password" };
                }

                return new { isValid = false, message = $"Metron returned unexpected status: {(int)response.StatusCode} {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to test Metron credentials");

                return new { isValid = false, message = $"Connection error: {ex.Message}" };
            }
        }

        [HttpGet("testcomicvine")]
        public object TestComicVineApiKey()
        {
            var apiKey = _configService.ComicVineApiKey;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new { isValid = false, message = "ComicVine API key not configured" };
            }

            try
            {
                var request = new HttpRequestBuilder("https://comicvine.gamespot.com/api/search/")
                    .AddQueryParam("api_key", apiKey)
                    .AddQueryParam("format", "json")
                    .AddQueryParam("resources", "volume")
                    .AddQueryParam("query", "test")
                    .SetHeader("User-Agent", "Panelarr/1.0")
                    .Build();

                request.SuppressHttpError = true;

                var response = _httpClient.Get(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return new { isValid = true };
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return new { isValid = false, message = "Invalid API key" };
                }

                return new { isValid = false, message = $"ComicVine returned unexpected status: {(int)response.StatusCode} {response.StatusCode}" };
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to test ComicVine API key");

                return new { isValid = false, message = $"Connection error: {ex.Message}" };
            }
        }
    }
}
