using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Aggregation.Aggregators
{
    public class AggregateQuality : IAggregate<LocalIssue>
    {
        public LocalIssue Aggregate(LocalIssue localTrack, bool otherFiles)
        {
            var quality = localTrack.FileTrackInfo?.Quality;

            if (quality == null)
            {
                quality = localTrack.FolderTrackInfo?.Quality;
            }

            if (quality == null)
            {
                quality = localTrack.DownloadClientIssueInfo?.Quality;
            }

            localTrack.Quality = quality;
            return localTrack;
        }
    }
}
