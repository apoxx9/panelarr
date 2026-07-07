using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Notifications.Komga
{
    public interface IKomgaProxy
    {
        void TriggerLibraryScan(KomgaSettings settings);
        ReaderPushResult PushCbl(KomgaSettings settings, string listName, byte[] cblData);
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
            var requestBuilder = BuildRequest(settings, "/api/v1/libraries/scan", HttpMethod.Post);
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

        public ReaderPushResult PushCbl(KomgaSettings settings, string listName, byte[] cblData)
        {
            // Komga has no one-shot CBL import: match/comicrack resolves the
            // entries to book ids, then the readlist is created (or updated —
            // creating a duplicate name is a 400) from those ids.
            var matchRequest = BuildRequest(settings, "/api/v1/readlists/match/comicrack", HttpMethod.Post);
            matchRequest.AddFormUpload("file", $"{listName}.cbl", cblData, "application/xml");

            var match = Deserialize<KomgaReadListMatch>(Execute(matchRequest, "Unable to match reading list against Komga"));

            if (match.ErrorCode.IsNotNullOrWhiteSpace())
            {
                throw new KomgaException($"Komga could not read the list: {match.ErrorCode}");
            }

            if (match.ReadListMatch != null && match.ReadListMatch.ErrorCode.IsNotNullOrWhiteSpace())
            {
                throw new KomgaException($"Komga could not read the list: {match.ReadListMatch.ErrorCode}");
            }

            var bookIds = new List<string>();
            var unmatched = new List<string>();

            foreach (var entry in match.Requests)
            {
                var label = DescribeEntry(entry.Request);

                if (entry.Matches.Count == 1 && entry.Matches[0].Books.Any())
                {
                    bookIds.Add(entry.Matches[0].Books[0].BookId);
                }
                else if (entry.Matches.Count > 1)
                {
                    unmatched.Add($"{label}: ambiguous ({entry.Matches.Count} series match)");
                }
                else
                {
                    unmatched.Add($"{label}: no match");
                }
            }

            if (bookIds.Empty())
            {
                throw new KomgaException("No entries matched anything in Komga's library");
            }

            var name = match.ReadListMatch?.Name;

            if (name.IsNullOrWhiteSpace())
            {
                name = listName;
            }

            var existing = FindReadListByName(settings, name);

            if (existing != null)
            {
                var updateRequest = BuildRequest(settings, $"/api/v1/readlists/{existing.Id}", HttpMethod.Patch);
                updateRequest.Headers.ContentType = "application/json";
                var patch = updateRequest.Build();
                patch.SetContent(new { BookIds = bookIds }.ToJson());

                Execute(patch, "Unable to update Komga reading list");
            }
            else
            {
                var createRequest = BuildRequest(settings, "/api/v1/readlists", HttpMethod.Post);
                createRequest.Headers.ContentType = "application/json";
                var post = createRequest.Build();
                post.SetContent(new
                {
                    Name = name,
                    Summary = string.Empty,
                    Ordered = true,
                    BookIds = bookIds
                }.ToJson());

                Execute(post, "Unable to create Komga reading list");
            }

            return new ReaderPushResult
            {
                Updated = existing != null,
                MatchedCount = bookIds.Count,
                Unmatched = unmatched
            };
        }

        private KomgaReadList FindReadListByName(KomgaSettings settings, string name)
        {
            var searchRequest = BuildRequest(settings, "/api/v1/readlists", HttpMethod.Get);
            searchRequest.AddQueryParam("search", name);
            searchRequest.AddQueryParam("unpaged", "true");

            var page = Deserialize<KomgaReadListPage>(Execute(searchRequest, "Unable to list Komga reading lists"));

            return page.Content.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private HttpResponse Execute(HttpRequestBuilder requestBuilder, string errorMessage)
        {
            return Execute(requestBuilder.Build(), errorMessage);
        }

        private HttpResponse Execute(HttpRequest request, string errorMessage)
        {
            try
            {
                return _httpClient.Execute(request);
            }
            catch (HttpException ex)
            {
                throw new KomgaException(errorMessage, ex);
            }
        }

        private static T Deserialize<T>(HttpResponse response)
        {
            var result = JsonSerializer.Deserialize<T>(response.Content);

            if (result == null)
            {
                throw new KomgaException("Komga returned an empty response");
            }

            return result;
        }

        private static string DescribeEntry(KomgaReadListMatchRequestBook request)
        {
            var series = request?.Series?.FirstOrDefault() ?? "Unknown series";

            return request?.Number.IsNotNullOrWhiteSpace() == true ? $"{series} #{request.Number}" : series;
        }

        private static HttpRequestBuilder BuildRequest(KomgaSettings settings, string path, HttpMethod method)
        {
            var baseUrl = settings.BaseUrl.TrimEnd('/');
            var requestBuilder = new HttpRequestBuilder(baseUrl + path)
            {
                Method = method
            };

            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{settings.Username}:{settings.Password}"));
            requestBuilder.Headers["Authorization"] = $"Basic {credentials}";

            return requestBuilder;
        }
    }
}
