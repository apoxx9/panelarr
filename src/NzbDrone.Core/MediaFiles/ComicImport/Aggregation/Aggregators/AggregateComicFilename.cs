using System.Collections.Generic;
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

            if (fileTrackInfo == null)
            {
                return localTrack;
            }

            var filename = Path.GetFileName(localTrack.Path);

            // Fill gaps from the filename only when embedded tags are missing
            if (fileTrackInfo.SeriesTitle.IsNullOrWhiteSpace() ||
                fileTrackInfo.IssueTitle.IsNullOrWhiteSpace())
            {
                ParseFilenameGaps(localTrack, filename);
            }

            // The collected-edition augmentation runs regardless: embedded
            // tags in these rips carry the bare line name as the series and
            // "vNN - Subtitle" as the title, which sinks identification just
            // as hard as an untagged filename does.
            AugmentCollectedEdition(fileTrackInfo, filename);

            return localTrack;
        }

        private void ParseFilenameGaps(LocalIssue localTrack, string filename)
        {
            var fileTrackInfo = localTrack.FileTagInfo;
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
        }

        /// <summary>
        /// A collected-edition name ("Iron Fist Epic Collection Vol. 01 -
        /// The Fury of Iron Fist (2015)...") defeats series matching twice
        /// over: the volume marker splits the library series name
        /// ("&lt;line&gt;: &lt;subtitle&gt;") and ComicParser truncates at the
        /// dash, losing the subtitle. Feed identification the full pre-year
        /// name and its marker-stripped form, with the volume number standing
        /// in for the issue number. The name comes from the filename AND from
        /// embedded tags - these rips tag the bare line name as the series
        /// with "vNN - Subtitle" as the title.
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

            var names = new List<string> { baseName };

            if (fileTrackInfo.SeriesTitle.IsNotNullOrWhiteSpace() && fileTrackInfo.IssueTitle.IsNotNullOrWhiteSpace())
            {
                names.Add(fileTrackInfo.SeriesTitle + " " + fileTrackInfo.IssueTitle);
            }

            var applied = false;

            foreach (var name in names)
            {
                var variants = NzbDrone.Core.Parser.Parser.GetCollectedEditionVariants(name, out var volumeNumber);

                if (!variants.Any())
                {
                    continue;
                }

                _logger.Debug(
                    "Collected-edition name '{0}': adding series variants [{1}], volume {2}",
                    name,
                    string.Join("; ", variants),
                    volumeNumber);

                foreach (var variant in variants)
                {
                    if (!fileTrackInfo.Series.Contains(variant, System.StringComparer.OrdinalIgnoreCase))
                    {
                        fileTrackInfo.Series.Add(variant);
                    }
                }

                applied = true;

                if (fileTrackInfo.SeriesIndex.IsNullOrWhiteSpace() && volumeNumber.IsNotNullOrWhiteSpace())
                {
                    fileTrackInfo.SeriesIndex = volumeNumber;
                }
            }

            if (applied)
            {
                fileTrackInfo.IsCollectedEdition = true;
            }
        }
    }
}
