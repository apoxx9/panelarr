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
            // Tracker/ruTracker - (Genre) [Source]? Series - Compendium/Collection with year range
            new Regex(@"^(?:\(.+?\))(?:\W*(?:\[(?<source>.+?)\]))?\W*(?<series>.+?)(?: - )(?<collection>Compendium|Complete\s+Omnibus|Complete\s+Series|Discography|Discografia).+?(?<startyear>\d{4}).+?(?<endyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Series - Compendium/Collection with two years
            new Regex(@"^(?<series>.+?)(?:\s+-\s+)(?:.+?)?(?<collection>Compendium|Complete\s+Omnibus|Complete\s+Series|Discography|Discografia).+?(?<startyear>\d{4}).+?(?<endyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Series - Compendium/Collection with end year
            new Regex(@"^(?<series>.+?)(?: - )(?:.+?)?(?<collection>Compendium|Complete\s+Omnibus|Complete\s+Series|Discography|Discografia).+?(?<endyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Tracker/ruTracker - (Genre) [Source]? Series - Compendium (no year range)
            new Regex(@"^(?:\(.+?\))(?:\W*(?:\[(?<source>.+?)\]))?\W*(?<series>.+?)(?: - )(?<collection>Compendium|Complete\s+Omnibus|Complete\s+Series|Discography|Discografia)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Series Compendium with volume: "Saga Compendium v01 (2019)"
            new Regex(@"^(?<series>.+?\s+(?<collection>Compendium))\s+v\d+",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Series Compendium/Collection with two years
            new Regex(@"^(?<series>.+?)\W*(?<collection>Compendium|Complete\s+Omnibus|Complete\s+Series|Discography|Discografia).+?(?<startyear>\d{4}).+?(?<endyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Series Compendium/Collection with end year
            new Regex(@"^(?<series>.+?)\W*(?<collection>Compendium|Complete\s+Omnibus|Complete\s+Series|Discography|Discografia).+?(?<endyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Series Compendium/Collection (no year)
            new Regex(@"^(?<series>.+?)\W*(?<collection>Compendium|Complete\s+Omnibus|Complete\s+Series|Discography|Discografia)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // GetComics/MAM - Title by Author [format] or (format)
            new Regex(@"^(?<issue>.+?)\s*(?:\(.+?\)\s*)*\bby\b\s*(?<series>.+?)(?:\[|\()",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Tracker/ruTracker - (Genre) [Source]? Series - Issue - Year, Format
            new Regex(@"^(?:\(.+?\))(?:\W*(?:\[(?<source>.+?)\]))?\W*(?<series>.+?)(?: - )(?<issue>.+?)(?:(?: - )(?<releaseyear>\d{4})|,)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Standard comic with issue number: Series NNN (Year) (Source) (Group).ext
            new Regex(@"^(?<series>.+?)\s+(?<issue>\d{1,4})\s+\((?<releaseyear>\d{4})\)\s+\((?<source>[^)]+)\)(?:\s+\((?<releasegroup>[^)]+)\))?",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Dot-separated comic: Series.Name.NNN.(Year).(Source).(Group).ext
            new Regex(@"^(?<series>[\w.-]+?)\.(?<issue>(?:[A-Za-z]+\.)?\d{1,4})\.\((?<releaseyear>\d{4})\)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Series - Title with volume: Hellboy Omnibus v01 - Seed of Destruction (Year)
            new Regex(@"^(?<series>.+?\s+v\d+)\s+-\s+(?<issue>.+?)\s+\((?<releaseyear>\d{4})\)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Series - Issue/Title (Year) (Source)
            new Regex(@"^(?<series>.+?)\s+-\s+(?<issue>.+?)\s+\((?<releaseyear>\d{4})\)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Series - Issue/Title - Year [something]
            new Regex(@"^(?:(?<series>.+?)(?: - )+)(?<issue>.+?)\W*(?: - )(?<releaseyear>\d{4})\W*(?:\(|\[)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Series - Issue/Title [something] or (something)
            new Regex(@"^(?:(?<series>.+?)(?: - )+)(?<issue>.+?)\W*(?:\(|\[)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Series - Issue/Title Year
            new Regex(@"^(?:(?<series>.+?)(?: - )+)(?<issue>.+?)\W*(?<releaseyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Dash-separated: Series-Issue-Year-Source
            new Regex(@"^(?:(?<series>.+?)(?:-)+)(?:(?<issue>.+?)(?:-)+)(?<releaseyear>\d{4})",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),

            // Dash-separated: Series-Year-Issue-Source
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

        public static ParsedFileTagInfo ParseFilePath(string path)
        {
            var fileInfo = new FileInfo(path);

            ParsedFileTagInfo result = null;

            Logger.Debug("Attempting to parse issue info using directory and file names. {0}", fileInfo.Directory.Name);
            result = ParseTitle(fileInfo.Directory.Name + " " + fileInfo.Name);

            if (result == null)
            {
                Logger.Debug("Attempting to parse issue info using directory name. {0}", fileInfo.Directory.Name);
                result = ParseTitle(fileInfo.Directory.Name + fileInfo.Extension);
            }

            return result;
        }

        public static ParsedFileTagInfo ParseTitle(string title)
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

        // Collected-edition volume marker inside a release title:
        // "Iron Fist Epic Collection Vol. 01 - The Fury of Iron Fist". The
        // marker splits the library series name ("<line>: <subtitle>") in
        // two, so the fuzzy find gets a second chance with it stripped.
        private static readonly Regex CollectedVolumeInfixRegex = new Regex(
            @"\b(?:vol(?:ume)?\.?\s*|v)(?<num>\d{1,4})\b[\s\-–—:]*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Series-name variants for a collected-edition name whose volume marker
        /// splits the library series name ("&lt;line&gt;: &lt;subtitle&gt;"):
        /// the raw name plus the marker-stripped form. Empty when the name has
        /// no volume marker; volumeNumber gets the marker's number with leading
        /// zeros trimmed so it compares equal to a stored issue number.
        /// </summary>
        public static List<string> GetCollectedEditionVariants(string name, out string volumeNumber)
        {
            volumeNumber = null;

            if (name.IsNullOrWhiteSpace())
            {
                return new List<string>();
            }

            name = name.Trim();
            var match = CollectedVolumeInfixRegex.Match(name);

            if (!match.Success)
            {
                return new List<string>();
            }

            volumeNumber = match.Groups["num"].Value.TrimStart('0');

            if (volumeNumber.Length == 0)
            {
                volumeNumber = "0";
            }

            var variants = new List<string> { name };
            var stripped = Regex.Replace(CollectedVolumeInfixRegex.Replace(name, " "), @"\s+", " ").Trim();

            if (stripped.IsNotNullOrWhiteSpace() && !stripped.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                variants.Add(stripped);
            }

            return variants;
        }

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

                    if (comicMatch.Groups["year"].Success)
                    {
                        comicResult.SeriesTitleInfo.Year = int.Parse(comicMatch.Groups["year"].Value);
                    }

                    comicResult.Quality = QualityParser.ParseQuality(title);

                    // A title that parses as a comic but carries no source tag is an
                    // archive of unknown source — never Unknown, so untagged releases
                    // and untagged files stay comparable. Keep the parsed revision.
                    if (comicResult.Quality?.Quality == Qualities.Quality.Unknown)
                    {
                        comicResult.Quality.Quality = Qualities.Quality.Archive;
                    }

                    Logger.Debug("Parsed comic title: {0} - {1} quality: {2}", comicSeriesName, comicResult.IssueTitle, comicResult.Quality);
                    return comicResult;
                }

                // Callers hand over whatever they have - a history row whose
                // series was deleted, or a null issue - and this must not
                // crash-and-swallow: no criteria means nothing to match against.
                if (series?.Name == null)
                {
                    Logger.Debug("No criteria series for '{0}', cannot parse with search criteria", title);
                    return null;
                }

                issues = issues?.Where(i => i != null).ToList() ?? new List<Issue>();

                if (!issues.Any())
                {
                    Logger.Debug("No criteria issues for '{0}', cannot parse with search criteria", title);
                    return null;
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

                var volMatch = CollectedVolumeInfixRegex.Match(simpleTitle);

                if (foundSeries == null && volMatch.Success)
                {
                    var strippedTitle = CollectedVolumeInfixRegex.Replace(simpleTitle, " ");

                    Logger.Trace($"Retrying with volume marker stripped: '{strippedTitle}'");
                    foundSeries = GetTitleFuzzy(strippedTitle, seriesName, out remainder);
                }

                var foundIssue = GetTitleFuzzy(remainder, bestIssue.Title, out _);

                if (foundIssue == null)
                {
                    foundIssue = GetTitleFuzzy(remainder, bestIssue.Title.SplitIssueTitle(seriesName).Item1, out _);
                }

                // The volume stand-ins below trust the series-name match to
                // have identified the volume. GetTitleFuzzy's lenient
                // threshold is not proof of that - one Epic line's subtitle
                // can drift onto another's release ("The Captain" aligned
                // onto "...Captain America Lives Again" and grabbed the wrong
                // book, whose "Vol. 1" then agreed with the issue number
                // every per-volume series carries). When the criteria series
                // has a subtitle, the release's post-marker segment must
                // match it AS A WHOLE - fuzzy containment cannot tell "The
                // Captain" from the front of "Captain America Lives Again".
                var strongSeriesMatch = false;

                if (foundSeries != null && volMatch.Success)
                {
                    var subtitleIndex = seriesName.IndexOf(':');

                    if (subtitleIndex > 0 && subtitleIndex < seriesName.Length - 1)
                    {
                        var afterMarker = simpleTitle.Substring(volMatch.Index + volMatch.Length);
                        afterMarker = Regex.Split(afterMarker, @"[\(\[]|\s+by\s+", RegexOptions.IgnoreCase)[0]
                            .Trim(' ', '-', '–', '—', ':');
                        var criteriaSubtitle = seriesName.Substring(subtitleIndex + 1).Trim();

                        strongSeriesMatch = afterMarker.IsNotNullOrWhiteSpace() &&
                                            afterMarker.ToLowerInvariant().FuzzyMatch(criteriaSubtitle.ToLowerInvariant()) >= 0.75;

                        // Some lines put the subtitle BEFORE the marker ("Aliens
                        // Epic Collection – The Original Years Vol. 3"), so the
                        // series match itself consumed it: what the matched span
                        // carries beyond the base name must then be the subtitle
                        // as a whole - the same whole-match bar, just applied to
                        // the other side of the marker
                        if (!strongSeriesMatch)
                        {
                            var baseName = seriesName.Substring(0, subtitleIndex).Trim().ToLowerInvariant();
                            var span = foundSeries.ToLowerInvariant();
                            var (baseStart, baseLength, _) = span.FuzzyMatch(baseName, 0.8, WordDelimiters);

                            if (baseStart != -1)
                            {
                                var beyondBase = span.Remove(baseStart, baseLength).Trim(' ', '-', '–', '—', ':');

                                strongSeriesMatch = beyondBase.IsNotNullOrWhiteSpace() &&
                                                    beyondBase.FuzzyMatch(criteriaSubtitle.ToLowerInvariant()) >= 0.75;
                            }
                        }
                    }
                    else
                    {
                        var strippedForCheck = CollectedVolumeInfixRegex.Replace(simpleTitle, " ");
                        strongSeriesMatch = strippedForCheck.ToLowerInvariant()
                            .FuzzyMatch(seriesName.ToLowerInvariant(), 0.85, WordDelimiters).Item1 != -1;
                    }
                }

                // A collected-edition volume number in the release title
                // ("Vol. 01", "v01") stands in for the issue-title match it
                // defeats - but only when it agrees with the target issue
                if (foundIssue == null && strongSeriesMatch &&
                    volMatch.Groups["num"].Value.TrimStart('0') == (bestIssue.IssueNumber ?? string.Empty).TrimStart('0'))
                {
                    foundIssue = bestIssue.Title;
                }

                // Per-volume series usually number their sole issue 1 while the
                // release carries the line's volume ("Vol. 11") - the marker
                // still stands in when it agrees with the issue TITLE
                // ("Volume 11") instead of the issue number
                if (foundIssue == null && strongSeriesMatch)
                {
                    var titleVolMatch = CollectedVolumeInfixRegex.Match(bestIssue.Title ?? string.Empty);

                    if (titleVolMatch.Success &&
                        titleVolMatch.Groups["num"].Value.TrimStart('0') == volMatch.Groups["num"].Value.TrimStart('0'))
                    {
                        foundIssue = bestIssue.Title;
                    }
                }

                // When the volume marker sits INSIDE the matched series name
                // (the remainder no longer carries it), the marker split the
                // series name and the subtitle alone identified the volume -
                // with a single target issue nothing else can be meant. A
                // marker after a plain series name stays in the remainder, so
                // an ongoing's trade cannot pose as its issue 1.
                if (foundIssue == null && strongSeriesMatch &&
                    issues.Count == 1 &&
                    !CollectedVolumeInfixRegex.IsMatch(remainder ?? string.Empty))
                {
                    foundIssue = bestIssue.Title;
                }

                Logger.Trace($"Found {foundSeries} - {foundIssue} with fuzzy parser");

                if (foundSeries == null || foundIssue == null)
                {
                    return null;
                }

                // The match was made AGAINST the criteria series' name, so
                // that name is the answer - not the matched span, which can
                // drop a leading "The", absorb a volume marker, or stop short
                // of the subtitle, all of which clean to names no library
                // series has and reject the release Unknown Series.
                foundSeries = series.Name == "Various Series" ? foundSeries : series.Name;

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

                    // Same rule as the simple-comic branch: a matched release
                    // with no source tag is an archive of unknown source -
                    // never Unknown, which no profile wants.
                    if (result.Quality?.Quality == Qualities.Quality.Unknown)
                    {
                        result.Quality.Quality = Qualities.Quality.Archive;
                    }

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

                    if (comicMatch.Groups["year"].Success)
                    {
                        result.SeriesTitleInfo.Year = int.Parse(comicMatch.Groups["year"].Value);
                    }

                    result.Quality = QualityParser.ParseQuality(title);

                    // Untagged comic release => archive of unknown source (see above)
                    if (result.Quality?.Quality == Quality.Unknown)
                    {
                        result.Quality.Quality = Quality.Archive;
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

        private static ParsedFileTagInfo ParseMatchMusicCollection(MatchCollection matchCollection)
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

            var result = new ParsedFileTagInfo();

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

            // Clean up dot-separated series names (e.g. "X-Men.Annual" -> "X-Men Annual")
            // but preserve intentional hyphens
            seriesName = seriesName.Trim('.', '-', ' ');
            issueTitle = issueTitle.Trim('.', '-', ' ');

            // Strip trailing volume indicator from series name (e.g. "Amazing Spider-Man v6" -> "Amazing Spider-Man")
            seriesName = Regex.Replace(seriesName, @"\s+v\d+$", "", RegexOptions.IgnoreCase).Trim();

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

            if (matchCollection[0].Groups["collection"].Success)
            {
                int.TryParse(matchCollection[0].Groups["startyear"].Value, out var discStart);
                int.TryParse(matchCollection[0].Groups["endyear"].Value, out var discEnd);
                result.IsCollection = true;

                if (discStart > 0 && discEnd > 0)
                {
                    result.CollectionStart = discStart;
                    result.CollectionEnd = discEnd;
                }
                else if (discEnd > 0)
                {
                    result.CollectionEnd = discEnd;
                }

                result.IssueTitle = "Complete Collection";
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
