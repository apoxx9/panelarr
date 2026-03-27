using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Books;
using NzbDrone.Core.Configuration;
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
            var metronUsername = _configService.MetronUsername;
            List<Series> metronResults = null;
            var metronFailed = false;

            if (!string.IsNullOrWhiteSpace(metronUsername))
            {
                try
                {
                    _logger.Info("Searching Metron for: {0}", title);
                    var results = _metronClient.SearchSeries(title);

                    if (results.Any())
                    {
                        _logger.Debug("Metron returned {0} results for: {1}", results.Count, title);
                        metronResults = MapMetronResults(results);
                    }
                    else
                    {
                        _logger.Debug("No results from Metron for: {0}", title);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to search Metron for: {0}", title);
                    metronFailed = true;
                }
            }
            else
            {
                _logger.Warn("Metron credentials not configured. Go to Settings > Metadata to set your Metron username and password.");
            }

            if (metronResults != null && metronResults.Any())
            {
                return metronResults;
            }

            // Fall back to ComicVine if Metron failed or returned no results
            var comicVineApiKey = _configService.ComicVineApiKey;

            if (!string.IsNullOrWhiteSpace(comicVineApiKey))
            {
                try
                {
                    _logger.Info("Falling back to ComicVine for: {0} (Metron {1})",
                        title,
                        metronFailed ? "failed" : "returned no results");

                    var cvResults = _comicVineClient.SearchSeries(title);

                    if (cvResults.Any())
                    {
                        _logger.Debug("ComicVine returned {0} results for: {1}", cvResults.Count, title);
                        return MapComicVineResults(cvResults);
                    }

                    _logger.Debug("No results from ComicVine for: {0}", title);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to search ComicVine for: {0}", title);
                }
            }
            else
            {
                _logger.Debug("ComicVine API key not configured, skipping fallback");
            }

            return new List<Series>();
        }

        public List<object> SearchForNewEntity(string title)
        {
            return SearchForNewSeries(title).Cast<object>().ToList();
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
                    ForeignPublisherId = r.Publisher?.Id.ToString()
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
                var providerSeries = new Provider.ProviderSeries
                {
                    ForeignSeriesId = "cv:" + r.Id,
                    Name = r.Name,
                    Year = int.TryParse(r.StartYear, out var y) ? y : (int?)null,
                    ForeignPublisherId = r.Publisher != null ? "cv:" + r.Publisher.Id : null,
                    ImageUrl = r.Image?.OriginalUrl ?? r.Image?.MediumUrl
                };

                var (metadata, series) = _mapper.MapSeries(providerSeries);

                _mapper.EnrichWithDbIds(series, metadata, existingSeries);

                return series;
            }).ToList();
        }
    }
}
