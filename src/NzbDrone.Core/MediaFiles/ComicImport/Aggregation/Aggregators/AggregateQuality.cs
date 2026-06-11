using System.IO;
using System.Linq;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.IssueImport.Aggregation.Aggregators
{
    public class AggregateQuality : IAggregate<LocalIssue>
    {
        public LocalIssue Aggregate(LocalIssue localTrack, bool otherFiles)
        {
            // Prefer the first source that detected a real quality. Comic release
            // names usually carry no format token, so folder/client titles parse to
            // a non-null Unknown — that must not mask the extension fallback.
            var candidates = new[]
            {
                localTrack.FileTrackInfo?.Quality,
                localTrack.FolderTrackInfo?.Quality,
                localTrack.DownloadClientIssueInfo?.Quality,
                QualityFromExtension(localTrack.Path)
            };

            localTrack.Quality = candidates.FirstOrDefault(q => q != null && q.Quality != Quality.Unknown)
                                 ?? candidates.FirstOrDefault(q => q != null)
                                 ?? new QualityModel(Quality.Unknown);

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
