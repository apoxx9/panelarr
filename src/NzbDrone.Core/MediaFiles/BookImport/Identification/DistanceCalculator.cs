using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Books;
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

        private static readonly List<string> EbookFormats = new List<string> { "Kindle Edition", "Nook", "ebook" };

        private static readonly List<string> AudiobookFormats = new List<string> { "Audiobook", "Audio CD", "Audio Cassette", "Audible Audio", "CD-ROM", "MP3 CD" };

        public static Distance IssueDistance(List<LocalBook> localTracks, Issue issue)
        {
            var dist = new Distance();

            // the most common list of authors reported by a file
            var fileSeriess = localTracks.Select(x => x.FileTrackInfo.Seriess.Where(a => a.IsNotNullOrWhiteSpace()).ToList())
                .GroupBy(x => x.ConcatToString())
                .OrderByDescending(x => x.Count())
                .First()
                .First();

            var authors = GetSeriesVariants(fileSeriess);

            dist.AddString("author", authors, issue.SeriesMetadata.Value.Name);
            Logger.Trace("author: '{0}' vs '{1}'; {2}", authors.ConcatToString("' or '"), issue.SeriesMetadata.Value.Name, dist.NormalizedDistance());

            var title = localTracks.MostCommon(x => x.FileTrackInfo.IssueTitle) ?? "";
            var titleOptions = new List<string> { issue.Title };
            if (titleOptions[0].Contains("#"))
            {
                titleOptions.Add(StripSeriesRegex.Replace(titleOptions[0]));
            }

            var (maintitle, _) = issue.Title.SplitBookTitle(issue.SeriesMetadata.Value.Name);
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

            dist.AddString("issue", fileTitles, titleOptions);
            Logger.Trace("issue: '{0}' vs '{1}'; {2}", fileTitles.ConcatToString("' or '"), titleOptions.ConcatToString("' or '"), dist.NormalizedDistance());

            // Year
            var localYear = localTracks.MostCommon(x => x.FileTrackInfo.Year);
            if (localYear > 0 && issue.ReleaseDate.HasValue)
            {
                var bookYear = issue.ReleaseDate?.Year ?? 0;
                if (localYear == bookYear)
                {
                    dist.Add("year", 0.0);
                }
                else
                {
                    var remoteYear = bookYear;
                    var diff = Math.Abs(localYear - remoteYear);
                    var diff_max = Math.Abs(DateTime.Now.Year - remoteYear);
                    dist.AddRatio("year", diff, diff_max);
                }

                Logger.Trace($"year: {localYear} vs {issue.ReleaseDate?.Year}; {dist.NormalizedDistance()}");
            }

            return dist;
        }

        public static List<string> GetSeriesVariants(List<string> fileSeriess)
        {
            var authors = new List<string>(fileSeriess);

            if (fileSeriess.Count == 1)
            {
                authors.AddRange(SplitSeries(fileSeriess[0]));
            }

            foreach (var author in fileSeriess)
            {
                if (author.Contains(','))
                {
                    var split = author.Split(',', 2).Select(x => x.Trim());
                    if (!split.First().Contains(' '))
                    {
                        authors.Add(split.Reverse().ConcatToString(" "));
                    }
                }
            }

            return authors;
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
