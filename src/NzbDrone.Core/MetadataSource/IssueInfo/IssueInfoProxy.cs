using System;
using System.Collections.Generic;
using System.Linq;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.Metron;
using NzbDrone.Core.MetadataSource.Provider;

namespace NzbDrone.Core.MetadataSource.IssueInfo
{
    public class IssueInfoProxy : IProvideSeriesInfo, IProvideIssueInfo, ISearchForNewIssue
    {
        private readonly ISeriesService _seriesService;
        private readonly IIssueService _issueService;
        private readonly Logger _logger;
        private readonly ICached<HashSet<string>> _cache;
        private readonly CachingService _seriesCache;
        private readonly IMetadataProvider _metadataProvider;
        private readonly IMetronMapper _metronMapper;

        public IssueInfoProxy(ISeriesService seriesService,
                             IIssueService issueService,
                             IMetadataProvider metadataProvider,
                             IMetronMapper metronMapper,
                             Logger logger,
                             ICacheManager cacheManager)
        {
            _seriesService = seriesService;
            _issueService = issueService;
            _metadataProvider = metadataProvider;
            _metronMapper = metronMapper;
            _cache = cacheManager.GetCache<HashSet<string>>(GetType());
            _logger = logger;

            _seriesCache = new CachingService(new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 })));
            _seriesCache.DefaultCachePolicy = new CacheDefaults
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
                    return _seriesCache.GetOrAdd(foreignSeriesId,
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
            catch (IssueInfoException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.Warn(e, "Unexpected error getting series info: {0}", foreignSeriesId);
                throw new IssueInfoException("Failed to get series info for {0}", e, foreignSeriesId);
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

            series.Issues = issues;
            series.SeriesGroups = new List<SeriesGroup>();

            var existingSeries = _seriesService.GetAllSeries();
            _metronMapper.EnrichWithDbIds(series, metadata, existingSeries);

            return series;
        }

        public HashSet<string> GetChangedIssues(DateTime startTime)
        {
            return _cache.Get("ChangedIssues", () => GetChangedIssuesUncached(startTime), TimeSpan.FromMinutes(30));
        }

        private HashSet<string> GetChangedIssuesUncached(DateTime startTime)
        {
            return null;
        }

        public Tuple<string, Issue, List<SeriesMetadata>> GetIssueInfo(string foreignIssueId)
        {
            try
            {
                _logger.Debug("Fetching issue info from provider for {0}", foreignIssueId);

                var providerIssue = _metadataProvider.GetIssueInfo(foreignIssueId);

                if (providerIssue == null)
                {
                    throw new IssueNotFoundException(foreignIssueId);
                }

                var issue = _metronMapper.MapIssue(providerIssue, 0);

                var dbIssue = _issueService.FindById(foreignIssueId);
                string seriesId;
                SeriesMetadata seriesMetadata;

                if (dbIssue != null)
                {
                    var series = _seriesService.GetSeriesByMetadataId(dbIssue.SeriesMetadataId);
                    seriesId = series?.ForeignSeriesId ?? foreignIssueId;
                    seriesMetadata = series?.Metadata.Value ?? new SeriesMetadata { ForeignSeriesId = foreignIssueId };
                }
                else
                {
                    seriesId = foreignIssueId;
                    seriesMetadata = new SeriesMetadata { ForeignSeriesId = foreignIssueId, Name = issue.Title };
                }

                issue.SeriesMetadata = seriesMetadata;

                return Tuple.Create(seriesId, issue, new List<SeriesMetadata> { seriesMetadata });
            }
            catch (IssueInfoException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.Warn(e, "Unexpected error getting issue info: {0}", foreignIssueId);
                throw new IssueInfoException("Failed to get issue info for {0}", e, foreignIssueId);
            }
        }

        public List<Issue> SearchForNewIssue(string title, string series, bool getAllEditions = true)
        {
            try
            {
                _logger.Debug("Searching for new issue: title={0}, series={1}", title, series);

                var query = title?.Trim() ?? string.Empty;
                if (series != null)
                {
                    query += " " + series;
                }

                var results = _metadataProvider.SearchSeries(query.Trim());
                if (results == null || !results.Any())
                {
                    return new List<Issue>();
                }

                var issues = new List<Issue>();
                foreach (var result in results)
                {
                    var (metadata, mappedSeries) = _metronMapper.MapSeries(result);
                    if (mappedSeries?.Issues?.Value != null)
                    {
                        foreach (var issue in mappedSeries.Issues.Value)
                        {
                            issue.SeriesMetadata = metadata;
                            issue.Series = mappedSeries;
                            issues.Add(issue);
                        }
                    }
                }

                return issues;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error searching for issue: {0}", title);
                return new List<Issue>();
            }
        }
    }
}
