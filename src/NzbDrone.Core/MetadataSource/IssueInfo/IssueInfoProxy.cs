using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Books;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Http;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.Goodreads;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class BookInfoProxy : IProvideSeriesInfo, IProvideBookInfo, ISearchForNewBook, ISearchForNewSeries, ISearchForNewEntity
    {
        private static readonly JsonSerializerOptions SerializerSettings = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false,
            Converters = { new STJUtcConverter() }
        };

        private readonly IHttpClient _httpClient;
        private readonly ICachedHttpResponseService _cachedHttpClient;
        private readonly IGoodreadsSearchProxy _goodreadsSearchProxy;
        private readonly ISeriesService _authorService;
        private readonly IBookService _bookService;
        private readonly Logger _logger;
        private readonly IMetadataRequestBuilder _requestBuilder;
        private readonly ICached<HashSet<string>> _cache;
        private readonly CachingService _authorCache;

        public BookInfoProxy(IHttpClient httpClient,
                             ICachedHttpResponseService cachedHttpClient,
                             IGoodreadsSearchProxy goodreadsSearchProxy,
                             ISeriesService authorService,
                             IBookService bookService,
                             IMetadataRequestBuilder requestBuilder,
                             Logger logger,
                             ICacheManager cacheManager)
        {
            _httpClient = httpClient;
            _cachedHttpClient = cachedHttpClient;
            _goodreadsSearchProxy = goodreadsSearchProxy;
            _authorService = authorService;
            _bookService = bookService;
            _requestBuilder = requestBuilder;
            _cache = cacheManager.GetCache<HashSet<string>>(GetType());
            _logger = logger;

            _authorCache = new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 })));
            _authorCache.DefaultCachePolicy = new CacheDefaults
            {
                DefaultCacheDurationSeconds = 60
            };
        }

        public HashSet<string> GetChangedSeries(DateTime startTime)
        {
            var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                .SetSegment("route", "author/changed")
                .AddQueryParam("since", startTime.ToString("o"))
                .Build();

            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Get<RecentUpdatesResource>(httpRequest);

            if (httpResponse.Resource == null || httpResponse.Resource.Limited)
            {
                return null;
            }

            return new HashSet<string>(httpResponse.Resource.Ids.Select(x => x.ToString()));
        }

        public Series GetSeriesInfo(string foreignSeriesId, bool useCache = true)
        {
            _logger.Debug("Getting Series details GoodreadsId of {0}", foreignSeriesId);

            try
            {
                if (useCache)
                {
                    return PollSeries(foreignSeriesId);
                }

                return PollSeriesUncached(foreignSeriesId);
            }
            catch (BookInfoException e)
            {
                _logger.Warn(e, "Unexpected error getting author info: {foreignSeriesId}", foreignSeriesId);
                throw;
            }
        }

        public HashSet<string> GetChangedBooks(DateTime startTime)
        {
            return _cache.Get("ChangedBooks", () => GetChangedBooksUncached(startTime), TimeSpan.FromMinutes(30));
        }

        private HashSet<string> GetChangedBooksUncached(DateTime startTime)
        {
            return null;
        }

        public Tuple<string, Issue, List<SeriesMetadata>> GetBookInfo(string foreignBookId)
        {
            try
            {
                return PollBook(foreignBookId);
            }
            catch (BookInfoException e)
            {
                _logger.Warn(e, "Unexpected error getting issue info: {foreignBookId}", foreignBookId);
                throw;
            }
        }

        public List<object> SearchForNewEntity(string title)
        {
            var issues = SearchForNewBook(title, null, false);

            var result = new List<object>();
            foreach (var issue in issues)
            {
                var author = issue.Series.Value;

                if (!result.Contains(author))
                {
                    result.Add(author);
                }

                result.Add(issue);
            }

            return result;
        }

        public List<Series> SearchForNewSeries(string title)
        {
            var issues = SearchForNewBook(title, null);

            return issues
                .Select(x => x.Series.Value)
                .DistinctBy(x => x.ForeignSeriesId)
                .ToList();
        }

        public List<Issue> SearchForNewBook(string title, string author, bool getAllEditions = true)
        {
            var q = title.ToLower().Trim();
            if (author != null)
            {
                q += " " + author;
            }

            try
            {
                var lowerTitle = title.ToLowerInvariant();

                var split = lowerTitle.Split(':');
                var prefix = split[0];

                if (split.Length == 2 && new[] { "author", "work", "edition", "isbn", "asin" }.Contains(prefix))
                {
                    var slug = split[1].Trim();

                    if (slug.IsNullOrWhiteSpace() || slug.Any(char.IsWhiteSpace))
                    {
                        return new List<Issue>();
                    }

                    if (prefix == "author" || prefix == "work" || prefix == "edition")
                    {
                        var isValid = int.TryParse(slug, out var searchId);
                        if (!isValid)
                        {
                            return new List<Issue>();
                        }

                        if (prefix == "author")
                        {
                            return SearchByGoodreadsSeriesId(searchId);
                        }

                        if (prefix == "work")
                        {
                            return SearchByGoodreadsWorkId(searchId);
                        }

                        if (prefix == "edition")
                        {
                            return SearchByGoodreadsBookId(searchId, getAllEditions);
                        }
                    }

                    // to handle isbn / asin
                    q = slug;
                }

                return Search(q, getAllEditions);
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex, ex.Message);
                throw new GoodreadsException("Search for '{0}' failed. Unable to communicate with Goodreads.", ex, title);
            }
            catch (Exception ex) when (ex is not BookInfoException)
            {
                _logger.Warn(ex, ex.Message);
                throw new GoodreadsException("Search for '{0}' failed. Invalid response received from Goodreads.", ex, title);
            }
        }

        public List<Issue> SearchByIsbn(string isbn)
        {
            return Search(isbn, true);
        }

        public List<Issue> SearchByAsin(string asin)
        {
            return Search(asin, true);
        }

        private List<Issue> Search(string query, bool getAllEditions)
        {
            List<SearchJsonResource> result;
            try
            {
                result = _goodreadsSearchProxy.Search(query);
            }
            catch (Exception e)
            {
                _logger.Warn(e, "Error searching for {0}", query);
                return new List<Issue>();
            }

            var issues = new List<Issue>();

            if (getAllEditions)
            {
                // Slower but more exhaustive, less intensive on metadata API
                var bookIds = result.Select(x => x.WorkId).ToList();

                var idMap = result.Select(x => new { SeriesId = x.Series.Id, IssueId = x.WorkId })
                    .GroupBy(x => x.SeriesId)
                    .ToDictionary(x => x.Key, x => x.Select(i => i.IssueId.ToString()).ToList());

                List<Issue> authorBooks;
                foreach (var author in idMap.Keys)
                {
                    authorBooks = SearchByGoodreadsSeriesId(author);
                    issues.AddRange(authorBooks.Where(b => idMap[author].Contains(b.ForeignIssueId)));
                }

                var missingBooks = bookIds.ExceptBy(x => x.ToString(), issues, x => x.ForeignIssueId, StringComparer.Ordinal).ToList();
                foreach (var issue in missingBooks)
                {
                    issues.AddRange(SearchByGoodreadsWorkId(issue));
                }

                return issues;
            }
            else
            {
                // Use sparingly, hits metadata API quite hard
                var ids = result.Select(x => x.IssueId).ToList();

                if (ids.Count == 0)
                {
                    return new List<Issue>();
                }

                if (ids.Count == 1)
                {
                    return SearchByGoodreadsBookId(ids[0], false);
                }

                try
                {
                    return MapSearchResult(ids);
                }
                catch (HttpException ex)
                {
                    _logger.Warn(ex);
                    throw new BookInfoException("Search for '{0}' failed. Unable to communicate with PanelarrAPI, returning status code: {1}.", ex, query, ex.Response.StatusCode);
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "Error mapping search results");

                    return new List<Issue>();
                }
            }
        }

        private List<Issue> SearchByGoodreadsSeriesId(int id)
        {
            try
            {
                var authorId = id.ToString();
                var result = GetSeriesInfo(authorId);
                var issues = result.Books.Value;
                var authors = new Dictionary<string, SeriesMetadata> { { authorId, result.Metadata.Value } };

                foreach (var issue in issues)
                {
                    AddDbIds(authorId, issue, authors);
                }

                return issues;
            }
            catch (SeriesNotFoundException)
            {
                return new List<Issue>();
            }
            catch (BookInfoException e)
            {
                _logger.Warn(e, "Error searching by author id");
                return new List<Issue>();
            }
        }

        public List<Issue> SearchByGoodreadsWorkId(int id)
        {
            try
            {
                var tuple = GetBookInfo(id.ToString());
                AddDbIds(tuple.Item1, tuple.Item2, tuple.Item3.ToDictionary(x => x.ForeignSeriesId));
                return new List<Issue> { tuple.Item2 };
            }
            catch (IssueNotFoundException)
            {
                return new List<Issue>();
            }
            catch (BookInfoException e)
            {
                _logger.Warn(e, "Error searching by work id");
                return new List<Issue>();
            }
        }

        public List<Issue> SearchByGoodreadsBookId(int id, bool getAllEditions)
        {
            try
            {
                var issue = GetEditionInfo(id, getAllEditions);

                return new List<Issue> { issue };
            }
            catch (SeriesNotFoundException)
            {
                return new List<Issue>();
            }
            catch (IssueNotFoundException)
            {
                return new List<Issue>();
            }
            catch (EditionNotFoundException)
            {
                return new List<Issue>();
            }
            catch (BookInfoException e)
            {
                _logger.Warn(e, "Error searching by issue id");
                return new List<Issue>();
            }
        }

        private Issue GetEditionInfo(int id, bool getAllEditions)
        {
            HttpRequest httpRequest;
            HttpResponse httpResponse;

            while (true)
            {
                httpRequest = _requestBuilder.GetRequestBuilder().Create()
                    .SetSegment("route", $"issue/{id}")
                    .Build();

                httpRequest.SuppressHttpError = true;

                // we expect a redirect
                httpResponse = _httpClient.Get(httpRequest);

                if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    WaitUntilRetry(httpResponse);
                }
                else
                {
                    break;
                }
            }

            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                throw new EditionNotFoundException(id.ToString());
            }

            if (!httpResponse.HasHttpRedirect)
            {
                throw new BookInfoException($"Unexpected response from {httpRequest.Url}");
            }

            var location = httpResponse.Headers.GetSingleValue("Location");
            var split = location.Split('/').Reverse().ToList();
            var newId = split[0];
            var type = split[1];

            Issue issue;
            List<SeriesMetadata> authors;

            if (type == "author")
            {
                var author = PollSeries(newId);

                issue = author.Books.Value.FirstOrDefault(b => b.ForeignIssueId == id.ToString());
                authors = new List<SeriesMetadata> { author.Metadata.Value };
            }
            else if (type == "work")
            {
                var tuple = PollBook(newId);

                issue = tuple.Item2;
                authors = tuple.Item3;
            }
            else
            {
                throw new NotImplementedException($"Unexpected response from {httpResponse.Request.Url}");
            }

            if (issue == null)
            {
                throw new EditionNotFoundException(id.ToString());
            }

            var authorDict = authors.ToDictionary(x => x.ForeignSeriesId);
            AddDbIds(issue.SeriesMetadata.Value.ForeignSeriesId, issue, authorDict);

            return issue;
        }

        private List<Issue> MapSearchResult(List<int> ids)
        {
            HttpResponse<BulkBookResource> httpResponse;

            while (true)
            {
                var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                    .SetSegment("route", "issue/bulk")
                    .SetHeader("Content-Type", "application/json")
                    .Build();

                httpRequest.SetContent(ids.ToJson());
                httpRequest.ContentSummary = ids.ToJson(Formatting.None);

                httpRequest.AllowAutoRedirect = true;
                httpRequest.SuppressHttpErrorStatusCodes = new[] { HttpStatusCode.TooManyRequests };

                httpResponse = _httpClient.Post<BulkBookResource>(httpRequest);

                if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    WaitUntilRetry(httpResponse);
                }
                else
                {
                    break;
                }
            }

            var mapped = MapBulkBook(httpResponse.Resource);

            var idStr = ids.Select(x => x.ToString()).ToList();

            return mapped.OrderBy(b => idStr.IndexOf(b.ForeignIssueId)).ToList();
        }

        private List<Issue> MapBulkBook(BulkBookResource resource)
        {
            var issues = new List<Issue>();

            if (resource == null)
            {
                return issues;
            }

            var authors = resource.Seriess.Select(MapSeriesMetadata).ToDictionary(x => x.ForeignSeriesId, x => x);
            var series = resource.SeriesGroup.Select(MapSeriesGroup).ToList();

            foreach (var work in resource.Works)
            {
                var issue = MapBook(work);
                var authorId = work.Books.OrderByDescending(b => b.AverageRating * b.RatingCount).First().Contributors.First().ForeignId.ToString();

                AddDbIds(authorId, issue, authors);

                issues.Add(issue);
            }

            MapSeriesLinks(series, issues, resource.SeriesGroup);

            return issues;
        }

        private void AddDbIds(string authorId, Issue issue, Dictionary<string, SeriesMetadata> authors)
        {
            var dbBook = _bookService.FindById(issue.ForeignIssueId);
            if (dbBook != null)
            {
                issue.UseDbFieldsFrom(dbBook);
            }

            var author = _authorService.FindById(authorId);

            if (author == null)
            {
                if (!authors.TryGetValue(authorId, out var metadata))
                {
                    throw new BookInfoException(string.Format("Expected author metadata for id [{0}] in issue data {1}", authorId, issue));
                }

                author = new Series
                {
                    CleanName = Parser.Parser.CleanSeriesName(metadata.Name),
                    Metadata = metadata
                };
            }

            issue.Series = author;
            issue.SeriesMetadata = author.Metadata.Value;
            issue.SeriesMetadataId = author.SeriesMetadataId;
        }

        private Series PollSeries(string foreignSeriesId)
        {
            return _authorCache.GetOrAdd(foreignSeriesId,
                () => PollSeriesUncached(foreignSeriesId),
                new LazyCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    ImmediateAbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    Size = 1,
                    SlidingExpiration = TimeSpan.FromMinutes(1),
                    ExpirationMode = ExpirationMode.ImmediateEviction
                }.RegisterPostEvictionCallback((key, value, reason, state) => _logger.Debug($"Clearing cache for {key} due to {reason}")));
        }

        private Series PollSeriesUncached(string foreignSeriesId)
        {
            SeriesResource resource = null;

            var useCache = true;

            for (var i = 0; i < 60; i++)
            {
                var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                    .SetSegment("route", $"author/{foreignSeriesId}")
                    .Build();

                httpRequest.AllowAutoRedirect = true;
                httpRequest.SuppressHttpError = true;

                var httpResponse = _cachedHttpClient.Get(httpRequest, useCache, TimeSpan.FromMinutes(30));

                if (httpResponse.HasHttpError)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        WaitUntilRetry(httpResponse);
                        continue;
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new SeriesNotFoundException(foreignSeriesId);
                    }
                    else if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        throw new BadRequestException(foreignSeriesId);
                    }
                    else
                    {
                        throw new BookInfoException("Unexpected error fetching author data");
                    }
                }

                resource = JsonSerializer.Deserialize<SeriesResource>(httpResponse.Content, SerializerSettings);

                if (resource.Works != null)
                {
                    resource.Works ??= new List<WorkResource>();
                    resource.SeriesGroup ??= new List<SeriesResource>();
                    break;
                }

                useCache = false;
                Thread.Sleep(2000);
            }

            if (resource?.Works == null)
            {
                throw new BookInfoException($"Failed to get works for {foreignSeriesId}");
            }

            return MapSeries(resource);
        }

        private Tuple<string, Issue, List<SeriesMetadata>> PollBook(string foreignBookId)
        {
            WorkResource resource = null;

            for (var i = 0; i < 60; i++)
            {
                var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                    .SetSegment("route", $"work/{foreignBookId}")
                    .Build();

                httpRequest.SuppressHttpError = true;

                // this may redirect to an author
                var httpResponse = _httpClient.Get(httpRequest);

                if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    WaitUntilRetry(httpResponse);
                    continue;
                }

                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new IssueNotFoundException(foreignBookId);
                }

                if (httpResponse.HasHttpRedirect)
                {
                    var location = httpResponse.Headers.GetSingleValue("Location");
                    var split = location.Split('/').Reverse().ToList();
                    var newId = split[0];
                    var type = split[1];

                    if (type == "author")
                    {
                        var author = PollSeries(newId);
                        var authorBook = author.Books.Value.SingleOrDefault(x => x.ForeignIssueId == foreignBookId);

                        if (authorBook == null)
                        {
                            throw new IssueNotFoundException(foreignBookId);
                        }

                        var authorMetadata = new List<SeriesMetadata> { author.Metadata.Value };

                        return Tuple.Create(author.ForeignSeriesId, authorBook, authorMetadata);
                    }
                    else
                    {
                        throw new NotImplementedException($"Unexpected response from {httpResponse.Request.Url}");
                    }
                }

                if (httpResponse.HasHttpError)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        throw new BadRequestException(foreignBookId);
                    }
                    else
                    {
                        throw new BookInfoException("Unexpected response fetching issue data");
                    }
                }

                resource = JsonSerializer.Deserialize<WorkResource>(httpResponse.Content, SerializerSettings);

                if (resource.Books != null)
                {
                    break;
                }

                Thread.Sleep(2000);
            }

            if (resource?.Books == null || resource?.Seriess == null || (!resource?.Seriess?.Any() ?? false))
            {
                throw new BookInfoException($"Failed to get issues for {foreignBookId}");
            }

            var issue = MapBook(resource);
            var authorId = GetSeriesId(resource).ToString();
            var metadata = resource.Seriess.Select(MapSeriesMetadata).ToList();

            var series = resource.SeriesGroup.Select(MapSeriesGroup).ToList();
            MapSeriesLinks(series, new List<Issue> { issue }, resource.SeriesGroup);

            return Tuple.Create(authorId, issue, metadata);
        }

        private void WaitUntilRetry(HttpResponse response)
        {
            var seconds = 5;

            if (response.Headers.ContainsKey("Retry-After"))
            {
                var retryAfter = response.Headers["Retry-After"];

                if (!int.TryParse(retryAfter, out seconds))
                {
                    seconds = 5;
                }
            }

            _logger.Info("BookInfo returned 429, backing off for {0}s", seconds);

            Thread.Sleep(TimeSpan.FromSeconds(seconds));
        }

        private static SeriesMetadata MapSeriesMetadata(SeriesResource resource)
        {
            var metadata = new SeriesMetadata
            {
                ForeignSeriesId = resource.ForeignId.ToString(),
                TitleSlug = resource.ForeignId.ToString(),
                Name = resource.Name.CleanSpaces(),
                Overview = resource.Description,
                Ratings = new Ratings { Votes = resource.RatingCount, Value = (decimal)resource.AverageRating },
                Status = SeriesStatusType.Continuing
            };

            metadata.SortName = metadata.Name.ToLower();

            if (resource.ImageUrl.IsNotNullOrWhiteSpace())
            {
                metadata.Images.Add(new MediaCover.MediaCover
                {
                    Url = resource.ImageUrl,
                    CoverType = MediaCoverTypes.Poster
                });
            }

            if (resource.Url.IsNotNullOrWhiteSpace())
            {
                metadata.Links.Add(new Links { Url = resource.Url, Name = "Goodreads" });
            }

            return metadata;
        }

        private static Series MapSeries(SeriesResource resource)
        {
            var metadata = MapSeriesMetadata(resource);

            var issues = resource.Works
                .Where(x => x.ForeignId > 0 && GetSeriesId(x) == resource.ForeignId)
                .Select(MapBook)
                .ToList();

            issues.ForEach(x => x.SeriesMetadata = metadata);

            var series = resource.SeriesGroup.Select(MapSeriesGroup).ToList();

            MapSeriesLinks(series, issues, resource.SeriesGroup);

            var result = new Series
            {
                Metadata = metadata,
                CleanName = Parser.Parser.CleanSeriesName(metadata.Name),
                Books = issues,
                SeriesGroups = series
            };

            return result;
        }

        private static void MapSeriesLinks(List<SeriesGroup> series, List<Issue> issues, List<SeriesResource> resource)
        {
            var bookDict = issues.ToDictionary(x => x.ForeignIssueId);
            var seriesDict = series.ToDictionary(x => x.ForeignSeriesId);

            foreach (var issue in issues)
            {
                issue.SeriesLinks = new List<SeriesGroupLink>();
            }

            // only take series where there are some works
            foreach (var s in resource.Where(x => x.LinkItems.Any()))
            {
                if (seriesDict.TryGetValue(s.ForeignId.ToString(), out var curr))
                {
                    curr.LinkItems = s.LinkItems.Where(x => x.ForeignWorkId != 0 && bookDict.ContainsKey(x.ForeignWorkId.ToString())).Select(l => new SeriesGroupLink
                    {
                        Issue = bookDict[l.ForeignWorkId.ToString()],
                        SeriesGroup = curr,
                        IsPrimary = l.Primary,
                        Position = l.PositionInSeries,
                        SeriesPosition = l.SeriesPosition
                    }).ToList();

                    foreach (var l in curr.LinkItems.Value)
                    {
                        l.Issue.Value.SeriesLinks.Value.Add(l);
                    }
                }
            }
        }

        private static SeriesGroup MapSeriesGroup(SeriesResource resource)
        {
            var series = new SeriesGroup
            {
                ForeignSeriesGroupId = resource.ForeignId.ToString(),
                Title = resource.Title ?? resource.Name,
                Description = resource.Description
            };

            return series;
        }

        private static Issue MapBook(WorkResource resource)
        {
            var issue = new Issue
            {
                ForeignIssueId = resource.ForeignId.ToString(),
                Title = resource.Title,
                TitleSlug = resource.ForeignId.ToString(),
                CleanTitle = Parser.Parser.CleanSeriesName(resource.Title),
                ReleaseDate = resource.ReleaseDate,
                Genres = resource.Genres
            };

            issue.Links.Add(new Links { Url = resource.Url, Name = "Goodreads" });

            // Use most popular book resource for metadata (title, ratings, release date)
            if (resource.Books != null && resource.Books.Any())
            {
                var mostPopular = resource.Books.MaxBy(x => x.AverageRating * x.RatingCount);
                if (mostPopular != null)
                {
                    // fix work title if missing
                    if (issue.Title.IsNullOrWhiteSpace())
                    {
                        issue.Title = mostPopular.Title;
                    }

                    // If we are missing the issue release date, set from books
                    if (!issue.ReleaseDate.HasValue && mostPopular.ReleaseDate.HasValue)
                    {
                        issue.ReleaseDate = mostPopular.ReleaseDate;
                    }

                    var ratingCount = resource.Books.Sum(x => x.RatingCount);
                    if (ratingCount > 0)
                    {
                        issue.Ratings = new Ratings
                        {
                            Votes = ratingCount,
                            Value = (decimal)(resource.Books.Sum(x => (double)x.RatingCount * x.AverageRating) / ratingCount)
                        };
                    }
                    else
                    {
                        issue.Ratings = new Ratings { Votes = 0, Value = 0 };
                    }
                }
            }
            else
            {
                issue.Ratings = new Ratings { Votes = 0, Value = 0 };
            }

            return issue;
        }

        private static int GetSeriesId(WorkResource b)
        {
            return b.Books.OrderByDescending(x => x.RatingCount * x.AverageRating).FirstOrDefault(x => x.Contributors.Any())?.Contributors.First().ForeignId ?? 0;
        }
    }
}
