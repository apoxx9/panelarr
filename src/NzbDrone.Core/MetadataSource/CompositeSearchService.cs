using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.MetadataSource.Metron;

namespace NzbDrone.Core.MetadataSource
{
    public class CompositeSearchService : ISearchForNewSeries, ISearchForNewEntity
    {
        private readonly IMetronApiClient _metronClient;
        private readonly IComicVineApiClient _comicVineClient;
        private readonly IMetronMapper _mapper;
        private readonly IConfigService _configService;
        private readonly ISeriesService _seriesService;
        private readonly Logger _logger;

        public CompositeSearchService(IMetronApiClient metronClient,
                                      IComicVineApiClient comicVineClient,
                                      IMetronMapper mapper,
                                      IConfigService configService,
                                      ISeriesService seriesService,
                                      Logger logger)
        {
            _metronClient = metronClient;
            _comicVineClient = comicVineClient;
            _mapper = mapper;
            _configService = configService;
            _seriesService = seriesService;
            _logger = logger;
        }

        public List<Series> SearchForNewSeries(string title)
        {
            // ComicVine first — richer search data (images, publisher, issue count)
            var comicVineApiKey = _configService.ComicVineApiKey;

            if (!string.IsNullOrWhiteSpace(comicVineApiKey))
            {
                try
                {
                    _logger.Info("Searching ComicVine for: {0}", title);
                    var cvResults = _comicVineClient.SearchSeries(title);

                    if (cvResults.Any())
                    {
                        _logger.Debug(
                            "ComicVine returned {0} results for: {1}",
                            cvResults.Count,
                            title);
                        return SortByRelevance(MapComicVineResults(cvResults), title);
                    }

                    _logger.Debug("No results from ComicVine for: {0}", title);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "ComicVine search failed for: {0}", title);
                }
            }

            // Fall back to Metron
            var metronUsername = _configService.MetronUsername;

            if (!string.IsNullOrWhiteSpace(metronUsername))
            {
                try
                {
                    _logger.Info("Searching Metron for: {0}", title);
                    var results = _metronClient.SearchSeries(title);

                    if (results.Any())
                    {
                        _logger.Debug(
                            "Metron returned {0} results for: {1}",
                            results.Count,
                            title);
                        return SortByRelevance(MapMetronResults(results), title);
                    }

                    _logger.Debug("No results from Metron for: {0}", title);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Metron search failed for: {0}", title);
                }
            }

            if (string.IsNullOrWhiteSpace(comicVineApiKey) && string.IsNullOrWhiteSpace(metronUsername))
            {
                _logger.Warn("No metadata providers configured.");
            }

            return new List<Series>();
        }

        public List<object> SearchForNewEntity(string title)
        {
            return SearchForNewSeries(title).Cast<object>().ToList();
        }

        private List<Series> SortByRelevance(List<Series> results, string query)
        {
            var queryLower = query.ToLowerInvariant().Trim();
            var queryWords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return results
                .Select(s => new { Series = s, Score = ScoreRelevance(s.Name?.ToLowerInvariant() ?? "", queryLower, queryWords) })
                .OrderByDescending(x => x.Score)
                .Select(x => x.Series)
                .ToList();
        }

        private static int ScoreRelevance(string name, string query, string[] queryWords)
        {
            // Exact match
            if (name == query)
            {
                return 100;
            }

            // Starts with query
            if (name.StartsWith(query))
            {
                return 80;
            }

            // Contains exact query as substring
            if (name.Contains(query))
            {
                return 60;
            }

            // Contains all query words
            if (queryWords.All(w => name.Contains(w)))
            {
                return 40;
            }

            // Contains some query words
            var matchCount = queryWords.Count(w => name.Contains(w));
            if (matchCount > 0)
            {
                return 10 + (matchCount * 10 / queryWords.Length);
            }

            return 0;
        }

        private List<Series> MapMetronResults(List<Metron.Resources.MetronSeriesListItem> results)
        {
            var existingSeries = _seriesService.GetAllSeries();

            return results.Select(r =>
            {
                var providerSeries = new Provider.ProviderSeries
                {
                    ForeignSeriesId = r.Id.ToString(),
                    Name = r.Name,
                    Year = r.YearBegan,
                    ForeignPublisherId = r.Publisher?.Id.ToString(),
                    PublisherName = r.Publisher?.Name
                };

                var (metadata, series) = _mapper.MapSeries(providerSeries);
                _mapper.EnrichWithDbIds(series, metadata, existingSeries);

                return series;
            }).ToList();
        }

        private List<Series> MapComicVineResults(List<ComicVine.Resources.ComicVineVolumeSummary> results)
        {
            var existingSeries = _seriesService.GetAllSeries();

            return results.Select(r =>
            {
                var pub = r.Publisher?.Name;
                var count = r.CountOfIssues;

                var providerSeries = new Provider.ProviderSeries
                {
                    ForeignSeriesId = "cv:" + r.Id,
                    Name = r.Name,
                    Year = int.TryParse(r.StartYear, out var y) ? y : (int?)null,
                    ForeignPublisherId = r.Publisher != null ? "cv:" + r.Publisher.Id : null,
                    PublisherName = count > 0 ? $"{pub}|{count}" : pub,
                    IssueCount = count,
                    Overview = r.Deck ?? r.Description,
                    ImageUrl = r.Image?.OriginalUrl ?? r.Image?.MediumUrl
                };

                var (metadata, series) = _mapper.MapSeries(providerSeries);
                _mapper.EnrichWithDbIds(series, metadata, existingSeries);

                return series;
            }).ToList();
        }
    }
}
