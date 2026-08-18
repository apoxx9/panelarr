using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.Provider;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.IssueImport.Identification
{
    public static class DistanceCalculator
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(DistanceCalculator));

        public static readonly List<string> VariousSeriesIds = new List<string> { "89ad4ac3-39f7-470e-963a-56509c546377" };

        private static readonly RegexReplace StripSeriesRegex = new RegexReplace(@"\([^\)].+?\)$", string.Empty, RegexOptions.Compiled);

        private static readonly RegexReplace CleanTitleCruft = new RegexReplace(@"\((?:unabridged)\)", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly List<string> AudiobookFormats = new List<string> { "Audiobook", "Audio CD", "Audio Cassette", "Audible Audio", "CD-ROM", "MP3 CD" };

        public static Distance IssueDistance(List<LocalIssue> localTracks, Issue issue, bool ignoreIssueNumber = false)
        {
            var dist = new Distance();

            // the most common list of allSeries reported by a file
            var fileSeries = localTracks.Select(x => x.FileTagInfo.Series.Where(a => a.IsNotNullOrWhiteSpace()).ToList())
                .GroupBy(x => x.ConcatToString())
                .OrderByDescending(x => x.Count())
                .First()
                .First();

            var allSeries = GetSeriesVariants(fileSeries);

            dist.AddString("series", allSeries, issue.SeriesMetadata.Value.Name);
            Logger.Trace("series: '{0}' vs '{1}'; {2}", allSeries.ConcatToString("' or '"), issue.SeriesMetadata.Value.Name, dist.NormalizedDistance());

            // Only compare issue titles when BOTH sides have a real one. An
            // empty local title surfaces as "" or a "#18"-style filename
            // placeholder — string-matching that against the provider's arc
            // title is a guaranteed max penalty that sank correct matches
            // just below the import threshold.
            var localTitle = localTracks.MostCommon(x => x.FileTagInfo.IssueTitle) ?? "";

            if (issue.Title.IsNotNullOrWhiteSpace() && HasComparableTitle(localTitle))
            {
                var title = localTitle;
                var titleOptions = new List<string> { issue.Title };
                if (titleOptions[0].Contains("#"))
                {
                    titleOptions.Add(StripSeriesRegex.Replace(titleOptions[0]));
                }

                var (maintitle, _) = issue.Title.SplitIssueTitle(issue.SeriesMetadata.Value.Name);
                if (!titleOptions.Contains(maintitle))
                {
                    titleOptions.Add(maintitle);
                }

                if (issue.SeriesLinks?.Value?.Any() ?? false)
                {
                    foreach (var l in issue.SeriesLinks.Value)
                    {
                        if (l.SeriesGroup?.Value?.Title?.IsNotNullOrWhiteSpace() ?? false)
                        {
                            titleOptions.Add($"{l.SeriesGroup.Value.Title} {l.Position} {issue.Title}");
                            titleOptions.Add($"{l.SeriesGroup.Value.Title} Issue {l.Position} {issue.Title}");
                            titleOptions.Add($"{issue.Title} {l.SeriesGroup.Value.Title} {l.Position}");
                            titleOptions.Add($"{issue.Title} {l.SeriesGroup.Value.Title} Issue {l.Position}");
                        }
                    }
                }

                var fileTitles = new[] { title, CleanTitleCruft.Replace(title) }.Distinct().ToList();

                // Collected editions title their sole issue "Volume N" while
                // rips title the file "vNN - Subtitle" or "Subtitle (Year)
                // (Digital) (Group)" - compare the local title with the marker
                // and the parenthetical junk stripped against the series
                // subtitle too, so the subtitle match the series was
                // identified by is not punished a second time at the title
                // layer.
                if (localTracks.Any(x => x.FileTagInfo.IsCollectedEdition))
                {
                    var parenIndex = title.IndexOf('(');
                    var preParen = parenIndex > 0 ? title.Substring(0, parenIndex).TrimEnd(' ', '-', '–', '—') : title;

                    if (preParen.IsNotNullOrWhiteSpace() && !fileTitles.Contains(preParen))
                    {
                        fileTitles.Add(preParen);
                    }

                    fileTitles.AddRange(Parser.Parser.GetCollectedEditionVariants(preParen, out _)
                        .Where(v => !fileTitles.Contains(v)));

                    var seriesName = issue.SeriesMetadata.Value.Name ?? string.Empty;
                    var subtitleIndex = seriesName.IndexOf(':');

                    if (subtitleIndex > 0 && subtitleIndex < seriesName.Length - 1)
                    {
                        var subtitle = seriesName.Substring(subtitleIndex + 1).Trim();

                        if (subtitle.IsNotNullOrWhiteSpace() && !titleOptions.Contains(subtitle))
                        {
                            titleOptions.Add(subtitle);
                        }
                    }
                }

                dist.AddString("issue", fileTitles, titleOptions);
                Logger.Trace("issue: '{0}' vs '{1}'; {2}", fileTitles.ConcatToString("' or '"), titleOptions.ConcatToString("' or '"), dist.NormalizedDistance());
            }

            // Issue number — the most reliable matching signal for comics.
            // Normalize both sides so padded ("003") and trailing-zero ("1.50")
            // forms compare equal, matching the candidate-lookup normalization.
            // Skipped for a sole-issue fallback candidate: the local number is
            // then a collected edition's line volume, not an issue index.
            var localIssueNumber = localTracks.MostCommon(x => x.FileTagInfo.SeriesIndex) ?? "";
            if (!ignoreIssueNumber && localIssueNumber.IsNotNullOrWhiteSpace())
            {
                var dbIssueNumber = issue.IssueNumber ?? "";
                dist.AddString("issue_number", NormalizeIssueNumber(localIssueNumber), NormalizeIssueNumber(dbIssueNumber));
                Logger.Trace("issue_number: '{0}' vs '{1}'; {2}", localIssueNumber, dbIssueNumber, dist.NormalizedDistance());
            }

            // Publisher — compare against the actual publisher name (never the
            // series name), and only when both sides have one. Taggers and the
            // provider disagree on alias forms ("DC" vs "DC Comics", "IDW" vs
            // "IDW Publishing"); containment means the same house.
            var localPublisher = localTracks.MostCommon(x => x.FileTagInfo.Publisher) ?? "";
            var dbPublisher = issue.SeriesMetadata?.Value?.Publisher?.Value?.Name;
            if (localPublisher.IsNotNullOrWhiteSpace() && dbPublisher.IsNotNullOrWhiteSpace())
            {
                if (PublishersMatch(localPublisher, dbPublisher))
                {
                    dist.Add("publisher", 0.0);
                }
                else
                {
                    dist.AddString("publisher", localPublisher, dbPublisher);
                }

                Logger.Trace("publisher: '{0}' vs '{1}'; {2}", localPublisher, dbPublisher, dist.NormalizedDistance());
            }

            // Year
            var localYear = localTracks.MostCommon(x => x.FileTagInfo.Year);
            if (localYear > 0 && issue.ReleaseDate.HasValue)
            {
                var issueYear = issue.ReleaseDate?.Year ?? 0;
                if (localYear == issueYear)
                {
                    dist.Add("year", 0.0);
                }
                else
                {
                    var remoteYear = issueYear;
                    var diff = Math.Abs(localYear - remoteYear);
                    var diff_max = Math.Abs(DateTime.Now.Year - remoteYear);
                    dist.AddRatio("year", diff, diff_max);
                }

                Logger.Trace($"year: {localYear} vs {issue.ReleaseDate?.Year}; {dist.NormalizedDistance()}");
            }

            return dist;
        }

        private static string NormalizeIssueNumber(string number)
        {
            return IssueNumberNormalizer.Normalize(number) ?? string.Empty;
        }

        // A title made only of digits, '#', whitespace and punctuation is a
        // filename-derived placeholder, not a real issue title
        private static bool HasComparableTitle(string title)
        {
            return title.IsNotNullOrWhiteSpace() &&
                   title.Any(c => char.IsLetter(c));
        }

        private static bool PublishersMatch(string a, string b)
        {
            var na = NormalizePublisher(a);
            var nb = NormalizePublisher(b);

            return na.Length > 0 && nb.Length > 0 &&
                   (na.Contains(nb) || nb.Contains(na));
        }

        private static string NormalizePublisher(string publisher)
        {
            return new string(publisher.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        }

        public static List<string> GetSeriesVariants(List<string> fileSeries)
        {
            var allSeries = new List<string>(fileSeries);

            if (fileSeries.Count == 1)
            {
                allSeries.AddRange(SplitSeries(fileSeries[0]));
            }

            foreach (var series in fileSeries)
            {
                if (series.Contains(','))
                {
                    var split = series.Split(',', 2).Select(x => x.Trim());
                    if (!split.First().Contains(' '))
                    {
                        allSeries.Add(split.Reverse().ConcatToString(" "));
                    }
                }
            }

            return allSeries;
        }

        private static List<string> SplitSeries(string input)
        {
            var seps = new[] { ';', '/' };
            foreach (var sep in seps)
            {
                if (input.Contains(sep))
                {
                    return input.Split(sep).Select(x => x.Trim()).ToList();
                }
            }

            var andSeps = new List<string> { " and ", " & " };
            foreach (var sep in andSeps)
            {
                if (input.Contains(sep))
                {
                    var result = new List<string>();
                    foreach (var s in input.Split(sep).Select(x => x.Trim()))
                    {
                        var s2 = SplitSeries(s);
                        if (s2.Any())
                        {
                            result.AddRange(s2);
                        }
                        else
                        {
                            result.Add(s);
                        }
                    }

                    return result;
                }
            }

            if (input.Contains(','))
            {
                var split = input.Split(',').Select(x => x.Trim()).ToList();
                if (split[0].Contains(' '))
                {
                    return split;
                }
            }

            return new List<string>();
        }
    }
}
