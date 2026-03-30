using System;
using System.Collections.Generic;
using System.Linq;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Books;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource.Metron;
using NzbDrone.Core.MetadataSource.Provider;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class BookInfoProxy : IProvideSeriesInfo, IProvideBookInfo, ISearchForNewBook
    {
        private readonly ISeriesService _authorService;
        private readonly IIssueService _bookService;
        private readonly Logger _logger;
        private readonly ICached<HashSet<string>> _cache;
        private readonly CachingService _authorCache;
        private readonly IMetadataProvider _metadataProvider;
        private readonly IMetronMapper _metronMapper;

        public BookInfoProxy(ISeriesService authorService,
                             IIssueService bookService,
                             IMetadataProvider metadataProvider,
                             IMetronMapper metronMapper,
                             Logger logger,
                             ICacheManager cacheManager)
        {
            _authorService = authorService;
            _bookService = bookService;
            _metadataProvider = metadataProvider;
            _metronMapper = metronMapper;
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
            return null;
        }

        public Series GetSeriesInfo(string foreignSeriesId, bool useCache = true)
        {
            _logger.Debug("Getting Series details for {0}", foreignSeriesId);

            try
            {
                if (useCache)
                {
                    return _authorCache.GetOrAdd(foreignSeriesId,
                        () => GetSeriesInfoFromProvider(foreignSeriesId),
                        new LazyCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                            ImmediateAbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                            Size = 1,
                            SlidingExpiration = TimeSpan.FromMinutes(1),
                            ExpirationMode = ExpirationMode.ImmediateEviction
                        });
                }

                return GetSeriesInfoFromProvider(foreignSeriesId);
            }
            catch (BookInfoException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.Warn(e, "Unexpected error getting series info: {0}", foreignSeriesId);
                throw new BookInfoException("Failed to get series info for {0}", e, foreignSeriesId);
            }
        }

        private Series GetSeriesInfoFromProvider(string foreignSeriesId)
        {
            _logger.Debug("Fetching series info from provider for {0}", foreignSeriesId);

            var providerSeries = _metadataProvider.GetSeriesInfo(foreignSeriesId);

            if (providerSeries == null)
            {
                throw new SeriesNotFoundException(foreignSeriesId);
            }

            var (metadata, series) = _metronMapper.MapSeries(providerSeries);

            var issues = new List<Issue>();
            if (providerSeries.Issues != null)
            {
                foreach (var providerIssue in providerSeries.Issues)
                {
                    var issue = _metronMapper.MapIssue(providerIssue, 0);
                    if (issue != null)
                    {
                        issue.SeriesMetadata = metadata;

                        if (issue.TitleSlug == null)
                        {
                            issue.TitleSlug = issue.ForeignIssueId ?? providerIssue.ForeignIssueId ?? "unknown";
                        }

                        issues.Add(issue);
                    }
                }
            }

            series.Books = issues;
            series.SeriesGroups = new List<SeriesGroup>();

            var existingSeries = _authorService.GetAllSeries();
            _metronMapper.EnrichWithDbIds(series, metadata, existingSeries);

            return series;
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
                _logger.Debug("Fetching issue info from provider for {0}", foreignBookId);

                var providerIssue = _metadataProvider.GetIssueInfo(foreignBookId);

                if (providerIssue == null)
                {
                    throw new IssueNotFoundException(foreignBookId);
                }

                var issue = _metronMapper.MapIssue(providerIssue, 0);

                var dbBook = _bookService.FindById(foreignBookId);
                string seriesId;
                SeriesMetadata seriesMetadata;

                if (dbBook != null)
                {
                    var author = _authorService.GetSeriesByMetadataId(dbBook.SeriesMetadataId);
                    seriesId = author?.ForeignSeriesId ?? foreignBookId;
                    seriesMetadata = author?.Metadata.Value ?? new SeriesMetadata { ForeignSeriesId = foreignBookId };
                }
                else
                {
                    seriesId = foreignBookId;
                    seriesMetadata = new SeriesMetadata { ForeignSeriesId = foreignBookId, Name = issue.Title };
                }

                issue.SeriesMetadata = seriesMetadata;

                return Tuple.Create(seriesId, issue, new List<SeriesMetadata> { seriesMetadata });
            }
            catch (BookInfoException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.Warn(e, "Unexpected error getting issue info: {0}", foreignBookId);
                throw new BookInfoException("Failed to get issue info for {0}", e, foreignBookId);
            }
        }

        public List<Issue> SearchForNewBook(string title, string author, bool getAllEditions = true)
        {
            try
            {
                _logger.Debug("Searching for new book: title={0}, author={1}", title, author);

                var query = title?.Trim() ?? string.Empty;
                if (author != null)
                {
                    query += " " + author;
                }

                var results = _metadataProvider.SearchSeries(query.Trim());
                if (results == null || !results.Any())
                {
                    return new List<Issue>();
                }

                var issues = new List<Issue>();
                foreach (var result in results)
                {
                    var (metadata, series) = _metronMapper.MapSeries(result);
                    if (series?.Books?.Value != null)
                    {
                        foreach (var issue in series.Books.Value)
                        {
                            issue.SeriesMetadata = metadata;
                            issue.Series = series;
                            issues.Add(issue);
                        }
                    }
                }

                return issues;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error searching for book: {0}", title);
                return new List<Issue>();
            }
        }
    }
}
