using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Aggregation.Aggregators
{
    /// <summary>
    /// Runs ComicParser on the filename to extract series title and issue number
    /// when embedded metadata (ComicInfo.xml) is missing. Handles comic naming
    /// conventions like "Batman #45.cbz" that the music-era AggregateFilenameInfo
    /// cannot parse.
    /// </summary>
    public class AggregateComicFilename : IAggregate<LocalIssue>
    {
        private readonly Logger _logger;

        public AggregateComicFilename(Logger logger)
        {
            _logger = logger;
        }

        public LocalIssue Aggregate(LocalIssue localTrack, bool otherFiles)
        {
            var fileTrackInfo = localTrack.FileTrackInfo;

            // Only augment when embedded tags are missing
            if (fileTrackInfo == null ||
                (fileTrackInfo.SeriesTitle.IsNotNullOrWhiteSpace() &&
                 fileTrackInfo.IssueTitle.IsNotNullOrWhiteSpace()))
            {
                return localTrack;
            }

            var filename = Path.GetFileName(localTrack.Path);
            var parsed = ComicParser.ParseRelease(filename);

            if (parsed == null)
            {
                return localTrack;
            }

            _logger.Debug(
                "Comic filename parse: '{0}' -> Series: '{1}', Issue: {2}",
                filename,
                parsed.SeriesTitle,
                parsed.IssueNumber);

            if (fileTrackInfo.SeriesTitle.IsNullOrWhiteSpace() && parsed.SeriesTitle.IsNotNullOrWhiteSpace())
            {
                fileTrackInfo.SeriesTitle = parsed.SeriesTitle;
                fileTrackInfo.Series = new[] { parsed.SeriesTitle }.ToList();
            }

            if (fileTrackInfo.IssueTitle.IsNullOrWhiteSpace() && parsed.IssueNumber.HasValue)
            {
                fileTrackInfo.IssueTitle = $"#{(int)parsed.IssueNumber.Value}";
            }

            if (parsed.IssueNumber.HasValue &&
                (!fileTrackInfo.TrackNumbers.Any() || fileTrackInfo.TrackNumbers.First() == 0))
            {
                fileTrackInfo.TrackNumbers = new[] { (int)parsed.IssueNumber.Value };

                if (fileTrackInfo.SeriesIndex.IsNullOrWhiteSpace())
                {
                    fileTrackInfo.SeriesIndex = ((int)parsed.IssueNumber.Value).ToString();
                }
            }

            return localTrack;
        }
    }
}
