using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Identification
{
    public interface ITrackGroupingService
    {
        List<LocalEdition> GroupTracks(List<LocalIssue> localTracks);
    }

    public class TrackGroupingService : ITrackGroupingService
    {
        private static readonly Logger _logger = NzbDroneLogger.GetLogger(typeof(TrackGroupingService));

        private static readonly List<string> VariousSeriesTitles = new () { "", "various series", "various", "va", "unknown" };

        // Every comic archive is a complete, self-contained release. The
        // music-era folder/tag grouping fused variants of the same issue into
        // a single edition, so only one of them could ever import — each file
        // must be identified and decided on its own.
        public List<LocalEdition> GroupTracks(List<LocalIssue> localTracks)
        {
            _logger.Debug("Treating {0} files as individual releases", localTracks.Count);

            return localTracks.Select(track => new LocalEdition(new List<LocalIssue> { track })).ToList();
        }

        public static bool IsVariousSeries(List<LocalIssue> tracks)
        {
            // checks whether most common title is a known VA title
            // Also checks whether more than 75% of tracks have a distinct series and that the most common series
            // is responsible for < 25% of tracks
            const double seriesTagThreshold = 0.75;
            const double tagFuzz = 0.9;

            var seriesTags = tracks.Select(x => x.FileTagInfo.SeriesTitle).ToList();

            if (!HasCommonEntry(seriesTags, seriesTagThreshold, tagFuzz))
            {
                return true;
            }

            if (VariousSeriesTitles.Contains(seriesTags.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool HasCommonEntry(IEnumerable<string> values, double threshold, double fuzz)
        {
            var groups = values.GroupBy(x => x).OrderByDescending(x => x.Count());
            var distinctCount = groups.Count();
            var mostCommonCount = groups.First().Count();
            var mostCommonEntry = groups.First().Key;
            var totalCount = values.Count();

            // merge groups that are close to the most common value
            foreach (var group in groups.Skip(1))
            {
                if (mostCommonEntry.IsNotNullOrWhiteSpace() &&
                    group.Key.IsNotNullOrWhiteSpace() &&
                    mostCommonEntry.LevenshteinCoefficient(group.Key) > fuzz)
                {
                    distinctCount--;
                    mostCommonCount += group.Count();
                }
            }

            _logger.Trace($"DistinctCount {distinctCount} MostCommonCount {mostCommonCount} TotalCout {totalCount}");

            if (distinctCount > 1 &&
                (distinctCount / (double)totalCount > threshold ||
                 mostCommonCount / (double)totalCount < 1 - threshold))
            {
                return false;
            }

            return true;
        }
    }
}
