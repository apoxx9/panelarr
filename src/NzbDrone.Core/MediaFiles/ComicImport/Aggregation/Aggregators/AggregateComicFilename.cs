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
            var fileTrackInfo = localTrack.FileTagInfo;

            // Only augment when embedded tags are missing
            if (fileTrackInfo == null ||
                (fileTrackInfo.SeriesTitle.IsNotNullOrWhiteSpace() &&
                 fileTrackInfo.IssueTitle.IsNotNullOrWhiteSpace()))
            {
                return localTrack;
            }

            var filename = Path.GetFileName(localTrack.Path);
            var parsed = ComicParser.ParseRelease(filename);

            if (parsed != null)
            {
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
                    (!fileTrackInfo.PartNumbers.Any() || fileTrackInfo.PartNumbers.First() == 0))
                {
                    fileTrackInfo.PartNumbers = new[] { (int)parsed.IssueNumber.Value };

                    if (fileTrackInfo.SeriesIndex.IsNullOrWhiteSpace())
                    {
                        fileTrackInfo.SeriesIndex = ((int)parsed.IssueNumber.Value).ToString();
                    }
                }
            }

            AugmentCollectedEdition(fileTrackInfo, filename);

            return localTrack;
        }

        /// <summary>
        /// A collected-edition filename ("Iron Fist Epic Collection Vol. 01 -
        /// The Fury of Iron Fist (2015)...") defeats series matching twice
        /// over: the volume marker splits the library series name
        /// ("&lt;line&gt;: &lt;subtitle&gt;") and ComicParser truncates at the
        /// dash, losing the subtitle. Feed identification the full pre-year
        /// name and its marker-stripped form, with the volume number standing
        /// in for the issue number.
        /// </summary>
        private void AugmentCollectedEdition(ParsedFileTagInfo fileTrackInfo, string filename)
        {
            var baseName = Path.GetFileNameWithoutExtension(filename);
            var parenIndex = baseName.IndexOf('(');

            if (parenIndex > 0)
            {
                baseName = baseName.Substring(0, parenIndex);
            }

            baseName = baseName.TrimEnd(' ', '-', '–', '—');

            var variants = NzbDrone.Core.Parser.Parser.GetCollectedEditionVariants(baseName, out var volumeNumber);

            if (!variants.Any())
            {
                return;
            }

            _logger.Debug(
                "Collected-edition filename: adding series variants [{0}], volume {1}",
                string.Join("; ", variants),
                volumeNumber);

            foreach (var variant in variants)
            {
                if (!fileTrackInfo.Series.Contains(variant, System.StringComparer.OrdinalIgnoreCase))
                {
                    fileTrackInfo.Series.Add(variant);
                }
            }

            if (fileTrackInfo.SeriesIndex.IsNullOrWhiteSpace() && volumeNumber.IsNotNullOrWhiteSpace())
            {
                fileTrackInfo.SeriesIndex = volumeNumber;
            }
        }
    }
}
