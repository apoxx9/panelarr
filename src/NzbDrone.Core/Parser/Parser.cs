using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser
{
    public static class Parser
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(Parser));

        private static readonly Regex[] ReportMusicTitleRegex = new[]
        {
            // Track with series (01 - series - trackName)
            new Regex(@"(?<trackNumber>\d*){0,1}([-| ]{0,1})(?<series>[a-zA-Z0-9, ().&_]*)[-| ]{0,1}(?<trackName>[a-zA-Z0-9, ().&_]+)",
                        RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Track without series (01 - trackName)
            new Regex(@"(?<trackNumber>\d*)[-| .]{0,1}(?<trackName>[a-zA-Z0-9, ().&_]+)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Track without trackNumber or series(trackName)
            new Regex(@"(?<trackNumber>\d*)[-| .]{0,1}(?<trackName>[a-zA-Z0-9, ().&_]+)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Track without trackNumber and  with series(series - trackName)
            new Regex(@"(?<trackNumber>\d*)[-| .]{0,1}(?<trackName>[a-zA-Z0-9, ().&_]+)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Track with series and starting title (01 - series - trackName)
            new Regex(@"(?<trackNumber>\d*){0,1}[-| ]{0,1}(?<series>[a-zA-Z0-9, ().&_]*)[-| ]{0,1}(?<trackName>[a-zA-Z0-9, ().&_]+)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
        };

        private static readonly Regex[] ReportIssueTitleRegex = new[]
        {
            //ruTracker - (Genre) [Source]? Series - Discography
            new Regex(@"^(?:\(.+?\))(?:\W*(?:\[(?<source>.+?)\]))?\W*(?<series>.+?)(?: - )(?<discography>Discography|Discografia).+?(?<startyear>\d{4}).+?(?<endyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series - Discography with two years
            new Regex(@"^(?<series>.+?)(?: - )(?:.+?)?(?<discography>Discography|Discografia).+?(?<startyear>\d{4}).+?(?<endyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series - Discography with end year
            new Regex(@"^(?<series>.+?)(?: - )(?:.+?)?(?<discography>Discography|Discografia).+?(?<endyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series Discography with two years
            new Regex(@"^(?<series>.+?)\W*(?<discography>Discography|Discografia).+?(?<startyear>\d{4}).+?(?<endyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series Discography with end year
            new Regex(@"^(?<series>.+?)\W*(?<discography>Discography|Discografia).+?(?<endyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series Discography
            new Regex(@"^(?<series>.+?)\W*(?<discography>Discography|Discografia)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //MyAnonaMouse - Title by Series [lang / pdf]
            new Regex(@"^(?<issue>.+)\bby\b(?<series>.+?)(?:\[|\()",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //ruTracker - (Genre) [Source]? Series - Issue - Year
            new Regex(@"^(?:\(.+?\))(?:\W*(?:\[(?<source>.+?)\]))?\W*(?<series>.+?)(?: - )(?<issue>.+?)(?: - )(?<releaseyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series-Issue-Version-Source-Year
            //ex. Imagine Dragons-Smoke And Mirrors-Deluxe Edition-2CD-FLAC-2015-JLM
            new Regex(@"^(?<series>.+?)[-](?<issue>.+?)[-](?:[\(|\[]?)(?<version>.+?(?:Edition)?)(?:[\)|\]]?)[-](?<source>\d?CD|WEB).+?(?<releaseyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series-Issue-Source-Year
            //ex. Dani_Sbert-Togheter-WEB-2017-FURY
            new Regex(@"^(?<series>.+?)[-](?<issue>.+?)[-](?<source>\d?CD|WEB).+?(?<releaseyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series - Issue (Year) Strict
            new Regex(@"^(?:(?<series>.+?)(?: - )+)(?<issue>.+?)\W*(?:\(|\[).+?(?<releaseyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series - Issue (Year)
            new Regex(@"^(?:(?<series>.+?)(?: - )+)(?<issue>.+?)\W*(?:\(|\[)(?<releaseyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series - Issue - Year [something]
            new Regex(@"^(?:(?<series>.+?)(?: - )+)(?<issue>.+?)\W*(?: - )(?<releaseyear>\d{4})\W*(?:\(|\[)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series - Issue [something] or Series - Issue (something)
            new Regex(@"^(?:(?<series>.+?)(?: - )+)(?<issue>.+?)\W*(?:\(|\[)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series - Issue Year
            new Regex(@"^(?:(?<series>.+?)(?: - )+)(?<issue>.+?)\W*(?<releaseyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series-Issue (Year) Strict
            //Hyphen no space between series and issue
            new Regex(@"^(?:(?<series>.+?)(?:-)+)(?<issue>.+?)\W*(?:\(|\[).+?(?<releaseyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series-Issue (Year)
            //Hyphen no space between series and issue
            new Regex(@"^(?:(?<series>.+?)(?:-)+)(?<issue>.+?)\W*(?:\(|\[)(?<releaseyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series-Issue [something] or Series-Issue (something)
            //Hyphen no space between series and issue
            new Regex(@"^(?:(?<series>.+?)(?:-)+)(?<issue>.+?)\W*(?:\(|\[)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series-Issue-something-Year
            new Regex(@"^(?:(?<series>.+?)(?:-)+)(?<issue>.+?)(?:-.+?)(?<releaseyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series-Issue Year
            //Hyphen no space between series and issue
            new Regex(@"^(?:(?<series>.+?)(?:-)+)(?:(?<issue>.+?)(?:-)+)(?<releaseyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            //Series - Year - Issue
            // Hypen with no or more spaces between series/issue/year
            new Regex(@"^(?:(?<series>.+?)(?:-))(?<releaseyear>\d{4})(?:-)(?<issue>[^-]+)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
        };

        private static readonly Regex[] RejectHashedReleasesRegex = new Regex[]
            {
                // Generic match for md5 and mixed-case hashes.
                new Regex(@"^[0-9a-zA-Z]{32}", RegexOptions.Compiled),

                // Generic match for shorter lower-case hashes.
                new Regex(@"^[a-z0-9]{24}$", RegexOptions.Compiled),

                // Format seen on some NZBGeek releases
                // Be very strict with these coz they are very close to the valid 101 ep numbering.
                new Regex(@"^[A-Z]{11}\d{3}$", RegexOptions.Compiled),
                new Regex(@"^[a-z]{12}\d{3}$", RegexOptions.Compiled),

                //Backup filename (Unknown origins)
                new Regex(@"^Backup_\d{5,}S\d{2}-\d{2}$", RegexOptions.Compiled),

                //123 - Started appearing December 2014
                new Regex(@"^123$", RegexOptions.Compiled),

                //abc - Started appearing January 2015
                new Regex(@"^abc$", RegexOptions.Compiled | RegexOptions.IgnoreCase),

                //b00bs - Started appearing January 2015
                new Regex(@"^b00bs$", RegexOptions.Compiled | RegexOptions.IgnoreCase)
            };

        private static readonly RegexReplace NormalizeRegex = new RegexReplace(@"((?:\b|_)(?<!^)(a(?!$)|an|the|and|or|of)(?!$)(?:\b|_))|\W|_",
                                                                string.Empty,
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PercentRegex = new Regex(@"(?<=\b\d+)%", RegexOptions.Compiled);

        private static readonly Regex FileExtensionRegex = new Regex(@"\.[a-z0-9]{2,4}$",
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly RegexReplace SimpleTitleRegex = new RegexReplace(@"(?:(480|720|1080|2160|320)[ip]|[xh][\W_]?26[45]|DD\W?5\W1|848x480|1280x720|1920x1080|3840x2160|4096x2160|(8|10)b(it)?)\s*",
                                                                string.Empty,
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Valid TLDs http://data.iana.org/TLD/tlds-alpha-by-domain.txt
        private static readonly RegexReplace WebsitePrefixRegex = new RegexReplace(@"^(?:\[\s*)?(?:www\.)?[-a-z0-9-]{1,256}\.(?:[a-z]{2,6}\.[a-z]{2,6}|xn--[a-z0-9-]{4,}|[a-z]{2,})\b(?:\s*\]|[ -]{2,})[ -]*",
                                                                string.Empty,
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly RegexReplace WebsitePostfixRegex = new RegexReplace(@"(?:\[\s*)?(?:www\.)?[-a-z0-9-]{1,256}\.(?:xn--[a-z0-9-]{4,}|[a-z]{2,6})\b(?:\s*\])$",
                                                                string.Empty,
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex AirDateRegex = new Regex(@"^(.*?)(?<!\d)((?<airyear>\d{4})[_.-](?<airmonth>[0-1][0-9])[_.-](?<airday>[0-3][0-9])|(?<airmonth>[0-1][0-9])[_.-](?<airday>[0-3][0-9])[_.-](?<airyear>\d{4}))(?!\d)",
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SixDigitAirDateRegex = new Regex(@"(?<=[_.-])(?<airdate>(?<!\d)(?<airyear>[1-9]\d{1})(?<airmonth>[0-1][0-9])(?<airday>[0-3][0-9]))(?=[_.-])",
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly RegexReplace CleanReleaseGroupRegex = new RegexReplace(@"^(.*?[-._ ])|(-(RP|1|NZBGeek|Obfuscated|Scrambled|sample|Pre|postbot|xpost|Rakuv[a-z0-9]*|WhiteRev|BUYMORE|AsRequested|AlternativeToRequested|GEROV|Z0iDS3N|Chamele0n|4P|4Planet))+$",
                                                                string.Empty,
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly RegexReplace CleanTorrentSuffixRegex = new RegexReplace(@"\[(?:ettv|rartv|rarbg|cttv)\]$",
                                                                string.Empty,
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ReleaseGroupRegex = new Regex(@"-(?<releasegroup>[a-z0-9]+)(?<!MP3|ALAC|FLAC|WEB)(?:\b|[-._ ])",
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex AnimeReleaseGroupRegex = new Regex(@"^(?:\[(?<subgroup>(?!\s).+?(?<!\s))\](?:_|-|\s|\.)?)",
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex YearInTitleRegex = new Regex(@"^(?<title>.+?)(?:\W|_)?(?<year>\d{4})",
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<char> WordDelimiters = new HashSet<char>(" .,_-=()[]|\"`'’");
        private static readonly Regex WordDelimiterRegex = new Regex(@"(\s|\.|,|_|-|=|\(|\)|\[|\]|\|)+", RegexOptions.Compiled);
        private static readonly Regex PunctuationRegex = new Regex(@"[^\w\s]", RegexOptions.Compiled);
        private static readonly Regex CommonWordRegex = new Regex(@"\b(a|an|the|and|or|of)\b\s?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SpecialEpisodeWordRegex = new Regex(@"\b(part|special|edition|christmas)\b\s?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DuplicateSpacesRegex = new Regex(@"\s{2,}", RegexOptions.Compiled);

        private static readonly Regex RequestInfoRegex = new Regex(@"\[.+?\]", RegexOptions.Compiled);

        private static readonly string[] Numbers = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };

        private static readonly Regex[] CommonTagRegex = new Regex[]
        {
            new Regex(@"(\[|\()*\b((featuring|feat.|feat|ft|ft.)\s{1}){1}\s*.*(\]|\))*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"(?:\(|\[)(?:[^\(\[]*)(?:version|limited|deluxe|single|clean|issue|special|bonus|promo|remastered)(?:[^\)\]]*)(?:\)|\])", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        private static readonly Regex[] BracketRegex = new Regex[]
        {
            new Regex(@"\(.*\)", RegexOptions.Compiled),
            new Regex(@"\[.*\]", RegexOptions.Compiled)
        };

        private static readonly Regex AfterDashRegex = new Regex(@"[-:].*", RegexOptions.Compiled);

        public static ParsedTrackInfo ParseMusicPath(string path)
        {
            var fileInfo = new FileInfo(path);

            ParsedTrackInfo result = null;

            Logger.Debug("Attempting to parse issue info using directory and file names. {0}", fileInfo.Directory.Name);
            result = ParseTitle(fileInfo.Directory.Name + " " + fileInfo.Name);

            if (result == null)
            {
                Logger.Debug("Attempting to parse issue info using directory name. {0}", fileInfo.Directory.Name);
                result = ParseTitle(fileInfo.Directory.Name + fileInfo.Extension);
            }

            return result;
        }

        public static ParsedTrackInfo ParseTitle(string title)
        {
            try
            {
                if (!ValidateBeforeParsing(title))
                {
                    return null;
                }

                Logger.Debug("Parsing string '{0}'", title);

                var releaseTitle = RemoveFileExtension(title);

                releaseTitle = releaseTitle.Replace("【", "[").Replace("】", "]");

                var simpleTitle = SimpleTitleRegex.Replace(releaseTitle);

                simpleTitle = WebsitePrefixRegex.Replace(simpleTitle);
                simpleTitle = WebsitePostfixRegex.Replace(simpleTitle);

                simpleTitle = CleanTorrentSuffixRegex.Replace(simpleTitle);

                var airDateMatch = AirDateRegex.Match(simpleTitle);
                if (airDateMatch.Success)
                {
                    simpleTitle = airDateMatch.Groups[1].Value + airDateMatch.Groups["airyear"].Value + "." + airDateMatch.Groups["airmonth"].Value + "." + airDateMatch.Groups["airday"].Value;
                }

                var sixDigitAirDateMatch = SixDigitAirDateRegex.Match(simpleTitle);
                if (sixDigitAirDateMatch.Success)
                {
                    var airYear = sixDigitAirDateMatch.Groups["airyear"].Value;
                    var airMonth = sixDigitAirDateMatch.Groups["airmonth"].Value;
                    var airDay = sixDigitAirDateMatch.Groups["airday"].Value;

                    if (airMonth != "00" || airDay != "00")
                    {
                        var fixedDate = string.Format("20{0}.{1}.{2}", airYear, airMonth, airDay);

                        simpleTitle = simpleTitle.Replace(sixDigitAirDateMatch.Groups["airdate"].Value, fixedDate);
                    }
                }

                foreach (var regex in ReportMusicTitleRegex)
                {
                    var match = regex.Matches(simpleTitle);

                    if (match.Count != 0)
                    {
                        Logger.Trace(regex);
                        try
                        {
                            var result = ParseMatchMusicCollection(match);

                            if (result != null)
                            {
                                result.Quality = QualityParser.ParseQuality(title);
                                Logger.Debug("Quality parsed: {0}", result.Quality);

                                return result;
                            }
                        }
                        catch (InvalidDateException ex)
                        {
                            Logger.Debug(ex, ex.Message);
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!title.ToLower().Contains("password") && !title.ToLower().Contains("yenc"))
                {
                    Logger.Error(e, "An error has occurred while trying to parse {0}", title);
                }
            }

            Logger.Debug("Unable to parse {0}", title);
            return null;
        }

        // Simple comic title pattern: "Series Name #N (Year)" or "Series Name #N"
        private static readonly Regex SimpleComicTitleRegex = new Regex(
            @"^(?<series>.+?)\s*#(?<issue>\d+)\s*(?:\((?<year>\d{4})\))?\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static ParsedIssueInfo ParseIssueTitleWithSearchCriteria(string title, Series series, List<Issue> issues)
        {
            try
            {
                if (!ValidateBeforeParsing(title))
                {
                    return null;
                }

                // Try simple comic title format first (GetComics style: "Series #N (Year)")
                var comicMatch = SimpleComicTitleRegex.Match(title);
                if (comicMatch.Success)
                {
                    var comicSeriesName = comicMatch.Groups["series"].Value.Trim();
                    var comicResult = new ParsedIssueInfo
                    {
                        SeriesName = comicSeriesName,
                        SeriesTitleInfo = GetSeriesTitleInfo(comicSeriesName),
                        IssueTitle = $"#{comicMatch.Groups["issue"].Value}"
                    };

                    comicResult.Quality = QualityParser.ParseQuality(title);

                    if (comicResult.Quality?.Quality == Qualities.Quality.Unknown)
                    {
                        comicResult.Quality = new QualityModel(Qualities.Quality.CBZ);
                    }

                    Logger.Debug("Parsed comic title: {0} - {1} quality: {2}", comicSeriesName, comicResult.IssueTitle, comicResult.Quality);
                    return comicResult;
                }

                var seriesName = series.Name == "Various Series" ? "VA" : series.Name.RemoveAccent();

                Logger.Debug("Parsing string '{0}' using search criteria series: '{1}' issues: '{2}'",
                             title,
                             seriesName.RemoveAccent(),
                             string.Join(", ", issues.Select(a => a.Title.RemoveAccent())));

                var releaseTitle = RemoveFileExtension(title);

                var simpleTitle = SimpleTitleRegex.Replace(releaseTitle);

                simpleTitle = WebsitePrefixRegex.Replace(simpleTitle);
                simpleTitle = WebsitePostfixRegex.Replace(simpleTitle);

                simpleTitle = CleanTorrentSuffixRegex.Replace(simpleTitle);

                var bestIssue = issues
                    .OrderByDescending(x => simpleTitle.FuzzyMatch(x.Title, wordDelimiters: WordDelimiters))
                    .First();

                var foundSeries = GetTitleFuzzy(simpleTitle, seriesName, out var remainder);

                if (foundSeries == null)
                {
                    foundSeries = GetTitleFuzzy(simpleTitle, seriesName.ToLastFirst(), out remainder);
                }

                var foundIssue = GetTitleFuzzy(remainder, bestIssue.Title, out _);

                if (foundIssue == null)
                {
                    foundIssue = GetTitleFuzzy(remainder, bestIssue.Title.SplitIssueTitle(seriesName).Item1, out _);
                }

                Logger.Trace($"Found {foundSeries} - {foundIssue} with fuzzy parser");

                if (foundSeries == null || foundIssue == null)
                {
                    return null;
                }

                var result = new ParsedIssueInfo
                {
                    SeriesName = foundSeries,
                    SeriesTitleInfo = GetSeriesTitleInfo(foundSeries),
                    IssueTitle = foundIssue
                };

                try
                {
                    result.Quality = QualityParser.ParseQuality(title);
                    Logger.Debug("Quality parsed: {0}", result.Quality);

                    result.ReleaseGroup = ParseReleaseGroup(releaseTitle);

                    Logger.Debug("Release Group parsed: {0}", result.ReleaseGroup);

                    return result;
                }
                catch (InvalidDateException ex)
                {
                    Logger.Debug(ex, ex.Message);
                }
            }
            catch (Exception e)
            {
                if (!title.ToLower().Contains("password") && !title.ToLower().Contains("yenc"))
                {
                    Logger.Error(e, "An error has occurred while trying to parse {0}", title);
                }
            }

            Logger.Debug("Unable to parse {0}", title);
            return null;
        }

        public static string GetTitleFuzzy(string report, string name, out string remainder)
        {
            remainder = report;

            Logger.Trace($"Finding '{name}' in '{report}'");

            var (locStart, matchLength, score) = report.ToLowerInvariant().FuzzyMatch(name.ToLowerInvariant(), 0.6, WordDelimiters);

            if (locStart == -1)
            {
                return null;
            }

            var found = report.Substring(locStart, matchLength);

            if (score >= 0.8)
            {
                remainder = report.Remove(locStart, matchLength);
                return found.Replace('.', ' ').Replace('_', ' ');
            }

            return null;
        }

        public static ParsedIssueInfo ParseIssueTitle(string title)
        {
            try
            {
                if (!ValidateBeforeParsing(title))
                {
                    return null;
                }

                Logger.Debug("Parsing string '{0}'", title);

                // Try simple comic title format first (GetComics style: "Series #N (Year)")
                var comicMatch = SimpleComicTitleRegex.Match(title);
                if (comicMatch.Success)
                {
                    var seriesName = comicMatch.Groups["series"].Value.Trim();
                    var result = new ParsedIssueInfo
                    {
                        SeriesName = seriesName,
                        SeriesTitleInfo = GetSeriesTitleInfo(seriesName),
                        IssueTitle = $"#{comicMatch.Groups["issue"].Value}"
                    };

                    result.Quality = QualityParser.ParseQuality(title);

                    if (result.Quality?.Quality == Quality.Unknown)
                    {
                        result.Quality = new QualityModel(Quality.CBZ);
                    }

                    return result;
                }

                var releaseTitle = RemoveFileExtension(title);

                var simpleTitle = SimpleTitleRegex.Replace(releaseTitle);

                simpleTitle = WebsitePrefixRegex.Replace(simpleTitle);
                simpleTitle = WebsitePostfixRegex.Replace(simpleTitle);

                simpleTitle = CleanTorrentSuffixRegex.Replace(simpleTitle);

                var airDateMatch = AirDateRegex.Match(simpleTitle);
                if (airDateMatch.Success)
                {
                    simpleTitle = airDateMatch.Groups[1].Value + airDateMatch.Groups["airyear"].Value + "." + airDateMatch.Groups["airmonth"].Value + "." + airDateMatch.Groups["airday"].Value;
                }

                var sixDigitAirDateMatch = SixDigitAirDateRegex.Match(simpleTitle);
                if (sixDigitAirDateMatch.Success)
                {
                    var airYear = sixDigitAirDateMatch.Groups["airyear"].Value;
                    var airMonth = sixDigitAirDateMatch.Groups["airmonth"].Value;
                    var airDay = sixDigitAirDateMatch.Groups["airday"].Value;

                    if (airMonth != "00" || airDay != "00")
                    {
                        var fixedDate = string.Format("20{0}.{1}.{2}", airYear, airMonth, airDay);

                        simpleTitle = simpleTitle.Replace(sixDigitAirDateMatch.Groups["airdate"].Value, fixedDate);
                    }
                }

                foreach (var regex in ReportIssueTitleRegex)
                {
                    var match = regex.Matches(simpleTitle);

                    if (match.Count != 0)
                    {
                        Logger.Trace(regex);
                        try
                        {
                            var result = ParseIssueMatchCollection(match, releaseTitle);

                            if (result != null)
                            {
                                result.Quality = QualityParser.ParseQuality(title);
                                Logger.Debug("Quality parsed: {0}", result.Quality);

                                result.ReleaseGroup = ParseReleaseGroup(releaseTitle);

                                var subGroup = GetSubGroup(match);
                                if (!subGroup.IsNullOrWhiteSpace())
                                {
                                    result.ReleaseGroup = subGroup;
                                }

                                Logger.Debug("Release Group parsed: {0}", result.ReleaseGroup);

                                result.ReleaseHash = GetReleaseHash(match);
                                if (!result.ReleaseHash.IsNullOrWhiteSpace())
                                {
                                    Logger.Debug("Release Hash parsed: {0}", result.ReleaseHash);
                                }

                                return result;
                            }
                        }
                        catch (InvalidDateException ex)
                        {
                            Logger.Debug(ex, ex.Message);
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!title.ToLower().Contains("password") && !title.ToLower().Contains("yenc"))
                {
                    Logger.Error(e, "An error has occurred while trying to parse {0}", title);
                }
            }

            Logger.Debug("Unable to parse {0}", title);
            return null;
        }

        public static (string, string) SplitIssueTitle(this string issue, string series)
        {
            // Strip series from title, eg Tom Clancy: Ghost Protocol
            if (issue.StartsWith($"{series}:"))
            {
                issue = issue.Split(':', 2)[1].Trim();
            }

            var parenthesis = issue.IndexOf('(');
            var colon = issue.IndexOf(':');

            string[] parts = null;

            if (parenthesis > -1)
            {
                var endParenthesis = issue.IndexOf(')', parenthesis);
                if (endParenthesis == -1 || !issue.Substring(parenthesis + 1, endParenthesis - parenthesis).Contains(' '))
                {
                    parenthesis = -1;
                }
            }

            if (colon > -1 && parenthesis > -1)
            {
                if (colon < parenthesis)
                {
                    parts = issue.Split(':', 2);
                }
                else
                {
                    parts = issue.Split('(', 2);
                    parts[1] = parts[1].TrimEnd(')');
                }
            }
            else if (colon > -1)
            {
                parts = issue.Split(':', 2);
            }
            else if (parenthesis > -1)
            {
                parts = issue.Split('(');
                parts[1] = parts[1].TrimEnd(')');
            }

            if (parts != null)
            {
                return (parts[0].Trim(), parts[1].TrimEnd(':').Trim());
            }

            return (issue, string.Empty);
        }

        public static string CleanSeriesName(this string name)
        {
            if (name.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            // If Title only contains numbers return it as is.
            if (long.TryParse(name, out _))
            {
                return name;
            }

            name = PercentRegex.Replace(name, "percent");

            return NormalizeRegex.Replace(name).ToLower().RemoveAccent();
        }

        public static string NormalizeTrackTitle(this string title)
        {
            title = SpecialEpisodeWordRegex.Replace(title, string.Empty);
            title = PunctuationRegex.Replace(title, " ");
            title = DuplicateSpacesRegex.Replace(title, " ");

            return title.Trim().ToLower();
        }

        public static string NormalizeTitle(string title)
        {
            title = WordDelimiterRegex.Replace(title, " ");
            title = PunctuationRegex.Replace(title, string.Empty);
            title = CommonWordRegex.Replace(title, string.Empty);
            title = DuplicateSpacesRegex.Replace(title, " ");

            return title.Trim().ToLower();
        }

        public static string ParseReleaseGroup(string title)
        {
            title = title.Trim();
            title = RemoveFileExtension(title);
            title = WebsitePrefixRegex.Replace(title);

            var animeMatch = AnimeReleaseGroupRegex.Match(title);

            if (animeMatch.Success)
            {
                return animeMatch.Groups["subgroup"].Value;
            }

            title = CleanReleaseGroupRegex.Replace(title);

            var matches = ReleaseGroupRegex.Matches(title);

            if (matches.Count != 0)
            {
                var group = matches.OfType<Match>().Last().Groups["releasegroup"].Value;

                if (int.TryParse(group, out _))
                {
                    return null;
                }

                return group;
            }

            return null;
        }

        public static string RemoveFileExtension(string title)
        {
            title = FileExtensionRegex.Replace(title, m =>
            {
                var extension = m.Value.ToLower();
                if (MediaFiles.MediaFileExtensions.AllExtensions.Contains(extension) || new[] { ".par2", ".nzb" }.Contains(extension))
                {
                    return string.Empty;
                }

                return m.Value;
            });

            return title;
        }

        public static string CleanIssueTitle(this string issue)
        {
            return CommonTagRegex[1].Replace(issue, string.Empty).Trim();
        }

        public static string RemoveBracketsAndContents(this string issue)
        {
            var intermediate = issue;
            foreach (var regex in BracketRegex)
            {
                intermediate = regex.Replace(intermediate, string.Empty).Trim();
            }

            return intermediate;
        }

        public static string RemoveAfterDash(this string text)
        {
            return AfterDashRegex.Replace(text, string.Empty).Trim();
        }

        public static string CleanTrackTitle(this string title)
        {
            var intermediateTitle = title;
            foreach (var regex in CommonTagRegex)
            {
                intermediateTitle = regex.Replace(intermediateTitle, string.Empty).Trim();
            }

            return intermediateTitle;
        }

        private static ParsedTrackInfo ParseMatchMusicCollection(MatchCollection matchCollection)
        {
            var seriesName = matchCollection[0].Groups["series"].Value./*Removed for cases like Will.I.Am Replace('.', ' ').*/Replace('_', ' ');
            seriesName = RequestInfoRegex.Replace(seriesName, "").Trim(' ');

            // Coppied from Radarr (https://github.com/Radarr/Radarr/blob/develop/src/NzbDrone.Core/Parser/Parser.cs)
            // TODO: Split into separate method and write unit tests for.
            var parts = seriesName.Split('.');
            seriesName = "";
            var n = 0;
            var previousAcronym = false;
            var nextPart = "";
            foreach (var part in parts)
            {
                if (parts.Length >= n + 2)
                {
                    nextPart = parts[n + 1];
                }

                if (part.Length == 1 && part.ToLower() != "a" && !int.TryParse(part, out n))
                {
                    seriesName += part + ".";
                    previousAcronym = true;
                }
                else if (part.ToLower() == "a" && (previousAcronym == true || nextPart.Length == 1))
                {
                    seriesName += part + ".";
                    previousAcronym = true;
                }
                else
                {
                    if (previousAcronym)
                    {
                        seriesName += " ";
                        previousAcronym = false;
                    }

                    seriesName += part + " ";
                }

                n++;
            }

            seriesName = seriesName.Trim(' ');

            var result = new ParsedTrackInfo();

            result.Series = new List<string> { seriesName };

            Logger.Debug("Track Parsed. {0}", result);
            return result;
        }

        private static SeriesTitleInfo GetSeriesTitleInfo(string title)
        {
            var seriesTitleInfo = new SeriesTitleInfo();
            seriesTitleInfo.Title = title;

            return seriesTitleInfo;
        }

        public static string ParseSeriesName(string title)
        {
            Logger.Debug("Parsing string '{0}'", title);

            var parseResult = ParseIssueTitle(title);

            if (parseResult == null)
            {
                return CleanSeriesName(title);
            }

            return parseResult.SeriesName;
        }

        private static ParsedIssueInfo ParseIssueMatchCollection(MatchCollection matchCollection, string releaseTitle)
        {
            var seriesName = matchCollection[0].Groups["series"].Value.Replace('.', ' ').Replace('_', ' ');
            var issueTitle = matchCollection[0].Groups["issue"].Value.Replace('.', ' ').Replace('_', ' ');
            var releaseVersion = matchCollection[0].Groups["version"].Value.Replace('.', ' ').Replace('_', ' ');
            seriesName = RequestInfoRegex.Replace(seriesName, "").Trim(' ');
            issueTitle = RequestInfoRegex.Replace(issueTitle, "").Trim(' ');
            releaseVersion = RequestInfoRegex.Replace(releaseVersion, "").Trim(' ');

            int.TryParse(matchCollection[0].Groups["releaseyear"].Value, out var releaseYear);

            ParsedIssueInfo result;

            result = new ParsedIssueInfo
            {
                ReleaseTitle = releaseTitle
            };

            result.SeriesName = seriesName;
            result.IssueTitle = issueTitle;
            result.SeriesTitleInfo = GetSeriesTitleInfo(result.SeriesName);
            result.ReleaseDate = releaseYear.ToString();
            result.ReleaseVersion = releaseVersion;

            if (matchCollection[0].Groups["discography"].Success)
            {
                int.TryParse(matchCollection[0].Groups["startyear"].Value, out var discStart);
                int.TryParse(matchCollection[0].Groups["endyear"].Value, out var discEnd);
                result.Discography = true;

                if (discStart > 0 && discEnd > 0)
                {
                    result.DiscographyStart = discStart;
                    result.DiscographyEnd = discEnd;
                }
                else if (discEnd > 0)
                {
                    result.DiscographyEnd = discEnd;
                }

                result.IssueTitle = "Discography";
            }

            Logger.Debug("Issue Parsed. {0}", result);

            return result;
        }

        private static bool ValidateBeforeParsing(string title)
        {
            if (title.ToLower().Contains("password") && title.ToLower().Contains("yenc"))
            {
                Logger.Debug("");
                return false;
            }

            if (!title.Any(char.IsLetterOrDigit))
            {
                return false;
            }

            var titleWithoutExtension = RemoveFileExtension(title);

            if (RejectHashedReleasesRegex.Any(v => v.IsMatch(titleWithoutExtension)))
            {
                Logger.Debug("Rejected Hashed Release Title: " + title);
                return false;
            }

            return true;
        }

        private static string GetSubGroup(MatchCollection matchCollection)
        {
            var subGroup = matchCollection[0].Groups["subgroup"];

            if (subGroup.Success)
            {
                return subGroup.Value;
            }

            return string.Empty;
        }

        private static string GetReleaseHash(MatchCollection matchCollection)
        {
            var hash = matchCollection[0].Groups["hash"];

            if (hash.Success)
            {
                var hashValue = hash.Value.Trim('[', ']');

                if (hashValue.Equals("1280x720"))
                {
                    return string.Empty;
                }

                return hashValue;
            }

            return string.Empty;
        }

        private static int ParseNumber(string value)
        {
            if (int.TryParse(value, out var number))
            {
                return number;
            }

            number = Array.IndexOf(Numbers, value.ToLower());

            if (number != -1)
            {
                return number;
            }

            throw new FormatException(string.Format("{0} isn't a number", value));
        }
    }
}
