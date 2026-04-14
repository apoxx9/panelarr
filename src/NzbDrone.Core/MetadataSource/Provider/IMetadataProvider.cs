using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.Provider
{
    /// <summary>
    /// Interface for comic metadata providers (e.g. Metron, ComicVine).
    /// </summary>
    public interface IMetadataProvider
    {
        /// <summary>Search for series by name.</summary>
        List<ProviderSeries> SearchSeries(string title);

        /// <summary>Get full series info including issue list by foreign series ID.</summary>
        ProviderSeries GetSeriesInfo(string foreignSeriesId);

        /// <summary>Get series IDs that have changed since the given epoch timestamp.</summary>
        List<string> GetChangedSeries(long epochSeconds);

        /// <summary>Get all issues for a series.</summary>
        List<ProviderIssue> GetIssues(string foreignSeriesId);

        /// <summary>Get full issue details by foreign issue ID.</summary>
        ProviderIssue GetIssueInfo(string foreignIssueId);

        /// <summary>Get publisher details by foreign publisher ID.</summary>
        ProviderPublisher GetPublisher(string foreignPublisherId);

        /// <summary>Get IDs of newly released issues since the given epoch timestamp.</summary>
        List<string> GetNewReleases(long epochSeconds);
    }
}
