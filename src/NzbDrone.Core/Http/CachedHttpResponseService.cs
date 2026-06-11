using System;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Http
{
    public interface ICachedHttpResponseService
    {
        HttpResponse Get(HttpRequest request, bool useCache, TimeSpan ttl);
        HttpResponse<T> Get<T>(HttpRequest request, bool useCache, TimeSpan ttl)
            where T : new();
    }

    public class CachedHttpResponseService : ICachedHttpResponseService
    {
        private readonly ICachedHttpResponseRepository _repo;
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public CachedHttpResponseService(ICachedHttpResponseRepository httpResponseRepository,
                                         IHttpClient httpClient,
                                         Logger logger)
        {
            _repo = httpResponseRepository;
            _httpClient = httpClient;
            _logger = logger;
        }

        // Credentials must not leak into (or fragment) the cache: the key is
        // the URL with any api key parameter redacted, so rotating a key
        // neither orphans rows nor stores the secret in the cache DB.
        private static readonly Regex ApiKeyRegex = new (@"(?<=\b(?:api_?key)=)[^&]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public HttpResponse Get(HttpRequest request, bool useCache, TimeSpan ttl)
        {
            var cacheKey = ApiKeyRegex.Replace(request.Url.ToString(), "REDACTED");
            var cached = _repo.FindByUrl(cacheKey);

            if (useCache && cached != null && cached.Expiry > DateTime.UtcNow)
            {
                _logger.Trace($"Returning cached response for [GET] {request.Url}");
                return new HttpResponse(request, new HttpHeader(), cached.Value, (HttpStatusCode)cached.StatusCode);
            }

            var result = _httpClient.Get(request);

            if (!result.HasHttpError)
            {
                if (cached == null)
                {
                    cached = new CachedHttpResponse
                    {
                        Url = cacheKey,
                    };
                }

                var now = DateTime.UtcNow;

                cached.LastRefresh = now;
                cached.Expiry = now.Add(ttl);
                cached.Value = result.Content;
                cached.StatusCode = (int)result.StatusCode;

                _repo.Upsert(cached);
            }

            return result;
        }

        public HttpResponse<T> Get<T>(HttpRequest request, bool useCache, TimeSpan ttl)
            where T : new()
        {
            var response = Get(request, useCache, ttl);
            return new HttpResponse<T>(response);
        }
    }
}
