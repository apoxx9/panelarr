using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource.ComicVine;
using NzbDrone.Core.MetadataSource.Metron;
using NzbDrone.Core.MetadataSource.Provider;

namespace NzbDrone.Core.MetadataSource
{
    /// <summary>
    /// Tries Metron first; falls back to ComicVine when Metron returns no results.
    /// ComicVine is only used when an ApiKey is configured.
    /// </summary>
    public class CompositeMetadataProvider : IMetadataProvider
    {
        private readonly MetronProvider _metron;
        private readonly ComicVineProvider _comicVine;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public CompositeMetadataProvider(MetronProvider metron,
                                         ComicVineProvider comicVine,
                                         IConfigService configService,
                                         Logger logger)
        {
            _metron = metron;
            _comicVine = comicVine;
            _configService = configService;
            _logger = logger;
        }

        private bool IsComicVineConfigured => !string.IsNullOrWhiteSpace(_configService.ComicVineApiKey);

        public List<ProviderSeries> SearchSeries(string title)
        {
            var results = _metron.SearchSeries(title);

            if (results.Any())
            {
                return results;
            }

            if (IsComicVineConfigured)
            {
                _logger.Debug("Metron returned no results for '{0}', falling back to ComicVine", title);
                return _comicVine.SearchSeries(title);
            }

            return results;
        }

        public ProviderSeries GetSeriesInfo(string foreignSeriesId)
        {
            if (IsCvId(foreignSeriesId))
            {
                return IsComicVineConfigured ? _comicVine.GetSeriesInfo(foreignSeriesId) : null;
            }

            return _metron.GetSeriesInfo(foreignSeriesId);
        }

        public List<string> GetChangedSeries(long epochSeconds)
        {
            return _metron.GetChangedSeries(epochSeconds);
        }

        public List<ProviderIssue> GetIssues(string foreignSeriesId)
        {
            if (IsCvId(foreignSeriesId))
            {
                return IsComicVineConfigured ? _comicVine.GetIssues(foreignSeriesId) : new List<ProviderIssue>();
            }

            return _metron.GetIssues(foreignSeriesId);
        }

        public ProviderIssue GetIssueInfo(string foreignIssueId)
        {
            if (IsCvId(foreignIssueId))
            {
                return IsComicVineConfigured ? _comicVine.GetIssueInfo(foreignIssueId) : null;
            }

            return _metron.GetIssueInfo(foreignIssueId);
        }

        public ProviderPublisher GetPublisher(string foreignPublisherId)
        {
            if (IsCvId(foreignPublisherId))
            {
                return IsComicVineConfigured ? _comicVine.GetPublisher(foreignPublisherId) : null;
            }

            return _metron.GetPublisher(foreignPublisherId);
        }

        public List<string> GetNewReleases(long epochSeconds)
        {
            return _metron.GetNewReleases(epochSeconds);
        }

        private static bool IsCvId(string foreignId)
        {
            return foreignId != null && foreignId.StartsWith("cv:");
        }
    }
}
