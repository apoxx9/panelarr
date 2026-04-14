using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.ImportLists.Panelarr
{
    public interface IPanelarrV1Proxy
    {
        List<PanelarrSeries> GetSeries(PanelarrSettings settings);
        List<PanelarrIssue> GetIssues(PanelarrSettings settings);
        List<PanelarrProfile> GetProfiles(PanelarrSettings settings);
        List<PanelarrRootFolder> GetRootFolders(PanelarrSettings settings);
        List<PanelarrTag> GetTags(PanelarrSettings settings);
        ValidationFailure Test(PanelarrSettings settings);
    }

    public class PanelarrV1Proxy : IPanelarrV1Proxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public PanelarrV1Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<PanelarrSeries> GetSeries(PanelarrSettings settings)
        {
            return Execute<PanelarrSeries>("/api/v1/series", settings);
        }

        public List<PanelarrIssue> GetIssues(PanelarrSettings settings)
        {
            return Execute<PanelarrIssue>("/api/v1/issue", settings);
        }

        public List<PanelarrProfile> GetProfiles(PanelarrSettings settings)
        {
            return Execute<PanelarrProfile>("/api/v1/qualityprofile", settings);
        }

        public List<PanelarrRootFolder> GetRootFolders(PanelarrSettings settings)
        {
            return Execute<PanelarrRootFolder>("api/v1/rootfolder", settings);
        }

        public List<PanelarrTag> GetTags(PanelarrSettings settings)
        {
            return Execute<PanelarrTag>("/api/v1/tag", settings);
        }

        public ValidationFailure Test(PanelarrSettings settings)
        {
            try
            {
                GetSeries(settings);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.Error(ex, "API Key is invalid");
                    return new ValidationFailure("ApiKey", "API Key is invalid");
                }

                if (ex.Response.HasHttpRedirect)
                {
                    _logger.Error(ex, "Panelarr returned redirect and is invalid");
                    return new ValidationFailure("BaseUrl", "Panelarr URL is invalid, are you missing a URL base?");
                }

                _logger.Error(ex, "Unable to connect to import list.");
                return new ValidationFailure(string.Empty, $"Unable to connect to import list: {ex.Message}. Check the log surrounding this error for details.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to connect to import list.");
                return new ValidationFailure(string.Empty, $"Unable to connect to import list: {ex.Message}. Check the log surrounding this error for details.");
            }

            return null;
        }

        private List<TResource> Execute<TResource>(string resource, PanelarrSettings settings)
        {
            if (settings.BaseUrl.IsNullOrWhiteSpace() || settings.ApiKey.IsNullOrWhiteSpace())
            {
                return new List<TResource>();
            }

            var baseUrl = settings.BaseUrl.TrimEnd('/');

            var request = new HttpRequestBuilder(baseUrl).Resource(resource)
                .Accept(HttpAccept.Json)
                .SetHeader("X-Api-Key", settings.ApiKey)
                .Build();

            var response = _httpClient.Get(request);

            if ((int)response.StatusCode >= 300)
            {
                throw new HttpException(response);
            }

            var results = JsonConvert.DeserializeObject<List<TResource>>(response.Content);

            return results;
        }
    }
}
