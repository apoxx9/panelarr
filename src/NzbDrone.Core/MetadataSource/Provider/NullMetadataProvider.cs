using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.Provider
{
    /// <summary>
    /// Null (no-op) implementation of IMetadataProvider.
    /// Used as default until a real provider (e.g. MetronProvider) is configured.
    /// </summary>
    public class NullMetadataProvider : IMetadataProvider
    {
        public List<ProviderSeries> SearchSeries(string title) => new List<ProviderSeries>();

        public ProviderSeries GetSeriesInfo(string foreignSeriesId) => null;

        public List<string> GetChangedSeries(long epochSeconds) => new List<string>();

        public List<ProviderIssue> GetIssues(string foreignSeriesId) => new List<ProviderIssue>();

        public ProviderIssue GetIssueInfo(string foreignIssueId) => null;

        public ProviderPublisher GetPublisher(string foreignPublisherId) => null;

        public List<string> GetNewReleases(long epochSeconds) => new List<string>();
    }
}
