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
            return SearchForNewSeries(new MetadataSearchCriteria(title));
        }

        public List<Series> SearchForNewSeries(MetadataSearchCriteria criteria)
        {
            var title = criteria.Term;

            // ComicVine first — use filter API for precise name matching
            var comicVineApiKey = _configService.ComicVineApiKey;

            if (!string.IsNullOrWhiteSpace(comicVineApiKey))
            {
                try
                {
                    _logger.Info("Searching ComicVine for: {0}{1}", title, criteria.Year.HasValue ? $" (year: {criteria.Year})" : "");

                    // Use /volumes filter API (like Mylar) for precise name matching
                    var cvResults = criteria.Year.HasValue
                        ? _comicVineClient.SearchVolumes(title, criteria.Year.Value)
                        : _comicVineClient.SearchVolumes(title);

                    // Fall back to /search if filter returns nothing (handles partial matches)
                    if (!cvResults.Any())
                    {
                        _logger.Debug("No filter results, falling back to search API for: {0}", title);
                        cvResults = _comicVineClient.SearchSeries(title);
                    }

                    if (cvResults.Any())
                    {
                        _logger.Debug("ComicVine returned {0} results for: {1}", cvResults.Count, title);
                        return SortByRelevance(MapComicVineResults(cvResults), title, criteria.Year);
                    }

                    _logger.Debug("No results from ComicVine for: {0}", title);
                }
                catch (NzbDrone.Common.Http.HttpException httpEx) when (httpEx.Response?.StatusCode == System.Net.HttpStatusCode.Unauthorized || httpEx.Response?.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.Error("ComicVine API key is invalid or expired. Please check your API key in Settings > Metadata.");
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
                        _logger.Debug("Metron returned {0} results for: {1}", results.Count, title);
                        return MapMetronResults(results);
                    }

                    _logger.Debug("No results from Metron for: {0}", title);
                }
                catch (NzbDrone.Common.Http.HttpException httpEx) when (httpEx.Response?.StatusCode == System.Net.HttpStatusCode.Unauthorized || httpEx.Response?.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.Error("Metron credentials are invalid. Please check your username and password in Settings > Metadata.");
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
            return SearchForNewEntity(new MetadataSearchCriteria(title));
        }

        public List<object> SearchForNewEntity(MetadataSearchCriteria criteria)
        {
            return SearchForNewSeries(criteria).Cast<object>().ToList();
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

        private List<Series> SortByRelevance(List<Series> results, string searchTerm, int? searchYear)
        {
            var term = searchTerm.ToLowerInvariant();

            return results.OrderBy(s =>
            {
                var name = (s.Name ?? string.Empty).ToLowerInvariant();

                // Exact match first
                if (name == term)
                {
                    return 0;
                }

                // Starts with search term
                if (name.StartsWith(term))
                {
                    return 1;
                }

                // Contains search term
                if (name.Contains(term))
                {
                    return 2;
                }

                return 3;
            })
            .ThenBy(s =>
            {
                // If year specified, prefer matching year
                if (searchYear.HasValue && s.Metadata?.Value?.Year.HasValue == true)
                {
                    return Math.Abs(s.Metadata.Value.Year.Value - searchYear.Value);
                }

                return 0;
            })
            .ToList();
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
