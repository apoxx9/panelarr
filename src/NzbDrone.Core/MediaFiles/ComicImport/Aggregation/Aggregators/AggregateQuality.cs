using System.IO;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

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

            if (quality == null)
            {
                quality = QualityFromExtension(localTrack.Path);
            }

            localTrack.Quality = quality ?? new QualityModel(Quality.Unknown);
            return localTrack;
        }

        private static QualityModel QualityFromExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var ext = Path.GetExtension(path)?.TrimStart('.').ToLowerInvariant();
            var quality = ext switch
            {
                "cbz" => Quality.CBZ,
                "cbr" => Quality.CBR,
                "cb7" => Quality.CB7,
                "pdf" => Quality.PDF,
                "epub" => Quality.EPUB,
                _ => null
            };

            return quality != null ? new QualityModel(quality) : null;
        }
    }
}
