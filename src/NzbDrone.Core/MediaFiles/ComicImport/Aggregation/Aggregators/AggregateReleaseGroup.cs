using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Aggregation.Aggregators
{
    public class AggregateReleaseGroup : IAggregate<LocalIssue>
    {
        public LocalIssue Aggregate(LocalIssue localTrack, bool otherFiles)
        {
            var releaseGroup = localTrack.DownloadClientIssueInfo?.ReleaseGroup;

            if (releaseGroup.IsNullOrWhiteSpace())
            {
                releaseGroup = localTrack.FolderTrackInfo?.ReleaseGroup;
            }

            if (releaseGroup.IsNullOrWhiteSpace())
            {
                releaseGroup = localTrack.FileTrackInfo?.ReleaseGroup;
            }

            localTrack.ReleaseGroup = releaseGroup;

            return localTrack;
        }
    }
}
