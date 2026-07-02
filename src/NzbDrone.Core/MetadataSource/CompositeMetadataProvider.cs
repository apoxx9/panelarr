using System;
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
        private bool IsMetronConfigured => !string.IsNullOrWhiteSpace(_configService.MetronUsername) &&
                                           !string.IsNullOrWhiteSpace(_configService.MetronPassword);

        public List<ProviderSeries> SearchSeries(string title)
        {
            var results = new List<ProviderSeries>();

            if (IsMetronConfigured)
            {
                try
                {
                    results = _metron.SearchSeries(title);
                }
                catch (Exception e)
                {
                    _logger.Warn(e, "Metron search failed for '{0}'", title);
                }
            }

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
                EnsureComicVineConfigured(foreignSeriesId);
                return _comicVine.GetSeriesInfo(foreignSeriesId);
            }

            return _metron.GetSeriesInfo(foreignSeriesId);
        }

        public List<string> GetChangedSeries(long epochSeconds)
        {
            // Neither provider supports deltas today (both return null). If one
            // gains support, a combined delta is only usable when BOTH providers
            // report — a null from either must make the whole result null, or
            // the other provider's series would be skipped as "unchanged".
            return _metron.GetChangedSeries(epochSeconds);
        }

        public List<ProviderIssue> GetIssues(string foreignSeriesId)
        {
            if (IsCvId(foreignSeriesId))
            {
                EnsureComicVineConfigured(foreignSeriesId);
                return _comicVine.GetIssues(foreignSeriesId);
            }

            return _metron.GetIssues(foreignSeriesId);
        }

        public ProviderIssue GetIssueInfo(string foreignIssueId)
        {
            if (IsCvId(foreignIssueId))
            {
                EnsureComicVineConfigured(foreignIssueId);
                return _comicVine.GetIssueInfo(foreignIssueId);
            }

            return _metron.GetIssueInfo(foreignIssueId);
        }

        public ProviderPublisher GetPublisher(string foreignPublisherId)
        {
            if (IsCvId(foreignPublisherId))
            {
                EnsureComicVineConfigured(foreignPublisherId);
                return _comicVine.GetPublisher(foreignPublisherId);
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

        // Returning null here would be indistinguishable from "not found at the provider",
        // which deletes file-less series on refresh. A missing key must surface as an error.
        private void EnsureComicVineConfigured(string foreignId)
        {
            if (!IsComicVineConfigured)
            {
                throw new IssueInfo.IssueInfoException("Cannot fetch '{0}': it is a ComicVine id but no ComicVine API key is configured", foreignId);
            }
        }
    }
}
