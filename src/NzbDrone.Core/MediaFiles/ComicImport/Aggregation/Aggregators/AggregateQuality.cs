using System.IO;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.MediaFiles.IssueImport.Aggregation.Aggregators
{
    public class AggregateQuality : IAggregate<LocalIssue>
    {
        public LocalIssue Aggregate(LocalIssue localTrack, bool otherFiles)
        {
            // Prefer the first source that detected a real quality: embedded
            // tags, the file's own name (source tags like "(Digital)"), then
            // folder/client titles, then the extension (an untagged archive).
            // Untagged names parse to a non-null Unknown, which must not mask
            // the later candidates.
            var fromFilename = QualityFromFilename(localTrack.Path);

            var candidates = new[]
            {
                localTrack.FileTagInfo?.Quality,
                fromFilename,
                localTrack.FolderTrackInfo?.Quality,
                localTrack.DownloadClientIssueInfo?.Quality,
                QualityFromExtension(localTrack.Path)
            };

            var quality = candidates.FirstOrDefault(q => q != null && q.Quality != Quality.Unknown)
                          ?? candidates.FirstOrDefault(q => q != null)
                          ?? new QualityModel(Quality.Unknown);

            // The file's own name is authoritative for fix markers — keep its
            // revision even when the quality came from another candidate
            // (e.g. "Saga 003 (f).cbz": Archive from the extension, v2 from "(f)").
            if (fromFilename != null && fromFilename.Revision.Version > quality.Revision.Version)
            {
                quality.Revision = fromFilename.Revision;
            }

            localTrack.Quality = quality;

            return localTrack;
        }

        private static QualityModel QualityFromFilename(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            // Strip the extension so ".cbz" can't read as an archive token and
            // shadow folder/client information.
            var name = Path.GetFileNameWithoutExtension(path);

            if (name.IsNullOrWhiteSpace())
            {
                return null;
            }

            var quality = QualityParser.ParseQualityModifiers(name, name.Replace('_', ' ').Trim().ToLower());
            quality.Quality = QualityParser.ParseSourceQuality(name);

            return quality;
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
                "cbz" => Quality.Archive,
                "cbr" => Quality.Archive,
                "cb7" => Quality.Archive,
                "pdf" => Quality.PDF,
                "epub" => Quality.EPUB,
                _ => null
            };

            return quality != null ? new QualityModel(quality) : null;
        }
    }
}
