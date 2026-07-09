using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser
{
    /// <summary>
    /// Parses comic release names into <see cref="ParsedComicInfo"/> instances.
    /// </summary>
    public static class ComicParser
    {
        private static readonly Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        // Issue number: #001, 001, Issue 1, No. 1, No 1, or bare 3-digit number after series title
        private static readonly Regex IssueNumberRegex = new Regex(
            @"(?:#|[Ii]ssue\s*|[Nn]o\.?\s*)(\d{1,4}(?:\.\d)?)",
            RegexOptions.Compiled);

        // Standalone 3-digit zero-padded number often used in scene releases: 001, 012
        private static readonly Regex SceneIssueNumberRegex = new Regex(
            @"\b(\d{3})\b(?!\s*\(of)",
            RegexOptions.Compiled);

        // Volume: v5, Vol. 3, Vol 03
        private static readonly Regex VolumeRegex = new Regex(
            @"\b(?:v|[Vv]ol\.?\s*)(\d{1,2})\b",
            RegexOptions.Compiled);

        // Year in parentheses: (2022), (2011-2012), (2016-)
        private static readonly Regex YearRegex = new Regex(
            @"\((\d{4})(?:\s*[-–]\s*\d{0,4})?\)",
            RegexOptions.Compiled);

        // Limited series: 01 (of 04), 01 of 04
        private static readonly Regex LimitedSeriesRegex = new Regex(
            @"(\d{1,3})\s*(?:\(of|of)\s*(\d{1,3})\)?",
            RegexOptions.Compiled);

        // Annual detection
        private static readonly Regex AnnualRegex = new Regex(
            @"\bannual\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Special detection
        private static readonly Regex SpecialRegex = new Regex(
            @"\bspecial\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // TPB/HC/Omnibus detection
        private static readonly Regex CollectedEditionRegex = new Regex(
            @"\b(?:TPB|Trade\s*Paperback|HC|Hardcover|Omnibus|Compendium|One.?Shot)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Source detection
        private static readonly Regex SourceRegex = new Regex(
            @"\b(?:Digital|Print|Scan|c2c|noads|Webrip|Web[-\s]?Rip)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Release group (typically last parenthetical containing known group indicators)
        private static readonly Regex ReleaseGroupRegex = new Regex(
            @"\(([^)]*(?:Empire|Minutemen|DCP|Mephisto|Digi(?!tal)|GetComics|Zone|Shan|GreenGiant|Novus|Senpai)(?:[^)]*)?)\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Generic last parenthetical for release group fallback
        private static readonly Regex LastParenthetical = new Regex(
            @"\(([^)]+)\)\s*$",
            RegexOptions.Compiled);

        // Square bracket sections: [CBZ], [ENG / CBR], [VIP], etc.
        private static readonly Regex SquareBracketRegex = new Regex(
            @"\[[^\]]*\]",
            RegexOptions.Compiled);

        // Format tag inside square brackets: [CBZ], [CBR], [PDF], [EPUB], [CB7]
        private static readonly Regex BracketFormatRegex = new Regex(
            @"\[(CBZ|CBR|CB7|PDF|EPUB)\]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // "by Author Name" suffix — word-boundary "by" followed by text, up to bracket/paren/end
        private static readonly Regex ByAuthorRegex = new Regex(
            @"\bby\s+[A-Z][^[\]()]*",
            RegexOptions.Compiled);

        // Issue range: "Issues 1-50", "Issues 1- 50", "Issue 1-25", "#1-5"
        private static readonly Regex IssueRangeRegex = new Regex(
            @"(?:\bIssues?\s+|#)\d+\s*[-–]\s*\d+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Multi-issue pack keywords: weekly 0-day bundles and explicit pack phrases.
        // Compound phrases only — a series legitimately titled "Pack" must not match.
        private static readonly Regex PackKeywordRegex = new Regex(
            @"\b(?:0[\s.-]?day|(?:weekly|comics?)\s+pack)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Numeric issue range: "001-016", "1-25", "v01-v05" — digits on both sides
        // of the dash so hyphenated titles ("X-23") and "Series - 66" never match
        private static readonly Regex SceneRangeRegex = new Regex(
            @"(?<![\d.])(\d{1,3})\s*[-–]\s*v?(\d{1,3})(?![\d.])",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Calendar dates ("2025-10-22", "2025 10.22") — stripped before range
        // detection so date-stamped titles don't read as issue ranges
        private static readonly Regex FullDateRegex = new Regex(
            @"\b\d{4}[-–. ]\d{1,2}[-–. ]\d{1,2}\b",
            RegexOptions.Compiled);

        // Known publisher prefixes that should be stripped from series titles
        private static readonly Regex PublisherPrefixRegex = new Regex(
            @"^(?:DC\s*Comics|Marvel(?:\s*Comics)?|Image(?:\s*Comics)?|Dark\s*Horse(?:\s*Comics)?|IDW(?:\s*Publishing)?|Boom!?\s*Studios|Dynamite(?:\s*Entertainment)?|Valiant(?:\s*Comics)?|Oni\s*Press|Vertigo|Aftershock|Titan\s*Comics|Archie\s*Comics)\s*[-–]\s*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static ParsedComicInfo ParseRelease(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return null;
            }

            var result = new ParsedComicInfo
            {
                ReleaseTitle = title
            };

            // Determine file format from extension if present
            var ext = Path.GetExtension(title)?.TrimStart('.').ToLower();
            result.Format = ext switch
            {
                "cbz" => ComicFormat.CBZ,
                "cbr" => ComicFormat.CBR,
                "cb7" => ComicFormat.CB7,
                "pdf" => ComicFormat.PDF,
                "epub" => ComicFormat.EPUB,
                _ => ComicFormat.Unknown
            };

            // Strip extension for parsing — avoid Path.GetFileNameWithoutExtension which
            // treats '/' as a directory separator, destroying series names like "Spider-Man/Deadpool"
            string name;
            if (result.Format != ComicFormat.Unknown)
            {
                var lastDot = title.LastIndexOf('.');
                name = lastDot > 0 ? title.Substring(0, lastDot) : title;
            }
            else
            {
                name = title;
            }

            // Normalize separators: underscores → spaces, dots → spaces (except fractional issues like 1.5)
            // Preserves dots between 1-2 digits (fractional issues), replaces all others
            name = name.Replace('_', ' ');
            name = Regex.Replace(name, @"(?<!\d)\.|\.(?!\d)|(?<=\d{3,})\.", " ");

            // Extract format from square bracket tags if not found from extension (e.g. "[CBZ]")
            if (result.Format == ComicFormat.Unknown)
            {
                var bracketFormat = BracketFormatRegex.Match(name);
                if (bracketFormat.Success)
                {
                    result.Format = bracketFormat.Groups[1].Value.ToUpper() switch
                    {
                        "CBZ" => ComicFormat.CBZ,
                        "CBR" => ComicFormat.CBR,
                        "CB7" => ComicFormat.CB7,
                        "PDF" => ComicFormat.PDF,
                        "EPUB" => ComicFormat.EPUB,
                        _ => ComicFormat.Unknown
                    };
                }
            }

            // Strip square bracket sections (metadata tags common in torrent releases)
            name = SquareBracketRegex.Replace(name, " ");

            // Strip "by Author" suffix (torrent naming convention)
            name = ByAuthorRegex.Replace(name, " ");

            // Normalize whitespace after stripping
            name = Regex.Replace(name, @"\s{2,}", " ").Trim();

            result.IsMultiIssue = IsMultiIssueName(name);

            // Extract year(s)
            var yearMatches = YearRegex.Matches(name);
            if (yearMatches.Count > 0)
            {
                if (int.TryParse(yearMatches[0].Groups[1].Value, out var year))
                {
                    result.Year = year;
                }
            }

            // Detect collected edition type
            var collectedMatch = CollectedEditionRegex.Match(name);
            if (collectedMatch.Success)
            {
                var token = collectedMatch.Value.ToLower();
                if (token.Contains("tpb") || token.Contains("trade"))
                {
                    result.IssueType = IssueType.TPB;
                }
                else if (token.Contains("hc") || token.Contains("hardcover"))
                {
                    result.IssueType = IssueType.Hardcover;
                }
                else if (token.Contains("omnibus") || token.Contains("compendium"))
                {
                    result.IssueType = IssueType.Omnibus;
                }
                else if (token.Contains("one") && token.Contains("shot"))
                {
                    result.IssueType = IssueType.OneShot;
                }
            }
            else if (AnnualRegex.IsMatch(name))
            {
                result.IssueType = IssueType.Annual;
            }
            else if (SpecialRegex.IsMatch(name))
            {
                result.IssueType = IssueType.Special;
            }
            else
            {
                result.IssueType = IssueType.Standard;
            }

            // Extract volume number
            var volumeMatch = VolumeRegex.Match(name);
            if (volumeMatch.Success && int.TryParse(volumeMatch.Groups[1].Value, out var vol))
            {
                result.VolumeNumber = vol;
            }

            // Extract limited series total
            var limitedMatch = LimitedSeriesRegex.Match(name);
            if (limitedMatch.Success && int.TryParse(limitedMatch.Groups[2].Value, out var total))
            {
                result.TotalIssues = total;
            }

            // Extract issue number (only for non-collected types)
            if (result.IssueType == IssueType.Standard || result.IssueType == IssueType.Annual ||
                result.IssueType == IssueType.Special || result.IssueType == IssueType.OneShot)
            {
                var issueMatch = IssueNumberRegex.Match(name);
                if (issueMatch.Success && float.TryParse(issueMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var issueNum))
                {
                    result.IssueNumber = issueNum;
                }
                else
                {
                    // Try scene-style 3-digit number - use the last match before a
                    // parenthetical year, since earlier numbers may be part of the
                    // series name (e.g. "100 Bullets 050 (2004)")
                    var sceneMatches = SceneIssueNumberRegex.Matches(name);
                    if (sceneMatches.Count > 0)
                    {
                        // Prefer the last match (closest to year/end of string)
                        var bestMatch = sceneMatches[sceneMatches.Count - 1];
                        if (float.TryParse(bestMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var sceneNum))
                        {
                            result.IssueNumber = sceneNum;
                        }
                    }
                }
            }

            // Extract source
            var sourceMatch = SourceRegex.Match(name);
            if (sourceMatch.Success)
            {
                result.Source = sourceMatch.Value;
            }

            // Extract release group
            var groupMatch = ReleaseGroupRegex.Match(name);
            if (groupMatch.Success)
            {
                result.ReleaseGroup = groupMatch.Groups[1].Value.Trim();
            }
            else
            {
                // Fallback: last parenthetical that isn't a year or "of N"
                var lastParen = LastParenthetical.Match(name);
                if (lastParen.Success)
                {
                    var candidate = lastParen.Groups[1].Value.Trim();
                    if (!Regex.IsMatch(candidate, @"^\d{4}$") && !candidate.ToLower().StartsWith("of "))
                    {
                        result.ReleaseGroup = candidate;
                    }
                }
            }

            // Bail out if no comic-specific signals were found — this avoids false positives
            // on music/audiobook release titles that would be handled by the generic parser.
            // A multi-issue pack marker (weekly 0-day bundle, issue range) counts as a signal:
            // those titles carry no year/issue/format but must reach the decision engine flagged.
            if (result.Format == ComicFormat.Unknown &&
                !result.Year.HasValue &&
                !result.IssueNumber.HasValue &&
                !result.VolumeNumber.HasValue &&
                result.Source == null &&
                !result.IsMultiIssue)
            {
                return null;
            }

            // Extract series title — everything before the first issue/volume marker or first parenthetical
            result.SeriesTitle = ExtractSeriesTitle(name, result);

            result.Quality = ParseQualityFrom(name, result);

            return result;
        }

        // Quality means source fidelity: the source tag ranks the release, the
        // container format only matters for PDF/EPUB (the format itself is the
        // limitation). A comic archive with no source tag is Quality.Archive —
        // never Unknown, so untagged releases and untagged files stay comparable.
        private static QualityModel ParseQualityFrom(string name, ParsedComicInfo parsed)
        {
            var quality = QualityParser.ParseQualityModifiers(name, name.Replace('_', ' ').Trim().ToLower());

            if (parsed.Format == ComicFormat.PDF)
            {
                quality.Quality = Quality.PDF;
                return quality;
            }

            if (parsed.Format == ComicFormat.EPUB)
            {
                quality.Quality = Quality.EPUB;
                return quality;
            }

            var source = QualityParser.ParseSourceQuality(name);

            quality.Quality = source == Quality.Unknown ? Quality.Archive : source;

            return quality;
        }

        // Bare year (not in parentheses): 2016, 1986, etc.
        private static readonly Regex BareYearRegex = new Regex(
            @"(?<!\d)\b(19|20)\d{2}\b(?!\))",
            RegexOptions.Compiled);

        // Regex for TPB/collected edition title extraction:
        // "Series TPB Vol NN - Subtitle" or "Series Vol. NN - Subtitle"
        private static readonly Regex TpbTitleRegex = new Regex(
            @"^(?<series>.+?)\s+(?:TPB|Trade\s*Paperback|HC|Hardcover|Omnibus)\s+(?:Vol\.?\s*\d+)\s*[-–]\s*(?<subtitle>.+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Also handle "Series Vol. NN - Subtitle" (without TPB marker)
        private static readonly Regex VolSubtitleRegex = new Regex(
            @"^(?<series>.+?)\s+Vol\.?\s*\d+\s*[-–]\s*(?<subtitle>.+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static string ExtractSeriesTitle(string name, ParsedComicInfo parsed)
        {
            // Remove all parentheticals for title extraction
            var cleaned = Regex.Replace(name, @"\([^)]*\)", " ").Trim();
            cleaned = Regex.Replace(cleaned, @"\s{2,}", " ").Trim();

            // For collected editions (TPB/HC/Omnibus), try to extract "Series - Subtitle"
            if (parsed.IssueType == IssueType.TPB || parsed.IssueType == IssueType.Hardcover ||
                parsed.IssueType == IssueType.Omnibus)
            {
                var tpbMatch = TpbTitleRegex.Match(cleaned);
                if (tpbMatch.Success)
                {
                    var series = tpbMatch.Groups["series"].Value.Trim();
                    var subtitle = tpbMatch.Groups["subtitle"].Value.Trim();
                    return $"{series} - {subtitle}".TrimEnd('-', '_', ' ');
                }

                var volMatch = VolSubtitleRegex.Match(cleaned);
                if (volMatch.Success)
                {
                    var series = volMatch.Groups["series"].Value.Trim();
                    var subtitle = volMatch.Groups["subtitle"].Value.Trim();
                    return $"{series} - {subtitle}".TrimEnd('-', '_', ' ');
                }
            }

            // For standard issues, find the earliest marker and take everything before it
            var cutoff = name.Length;

            // Check for parentheticals
            var parenIndex = name.IndexOf('(');
            if (parenIndex >= 0 && parenIndex < cutoff)
            {
                cutoff = parenIndex;
            }

            // Check for volume indicators
            var volumeMatch = VolumeRegex.Match(name);
            if (volumeMatch.Success && volumeMatch.Index < cutoff)
            {
                cutoff = volumeMatch.Index;
            }

            // Check for issue number patterns (#001, Issue 1, etc.)
            var issueMatch = IssueNumberRegex.Match(name);
            if (issueMatch.Success && issueMatch.Index < cutoff)
            {
                cutoff = issueMatch.Index;
            }

            // Check for issue range patterns (Issues 1-50, Issues 1- 50)
            var issueRangeMatch = IssueRangeRegex.Match(name);
            if (issueRangeMatch.Success && issueRangeMatch.Index < cutoff)
            {
                cutoff = issueRangeMatch.Index;
            }

            // Check for scene-style 3-digit number — but only the one that was
            // actually used as the issue number (to avoid stripping numbers that
            // are part of the title like "100" in "100 Bullets")
            if (parsed.IssueNumber.HasValue)
            {
                var sceneMatches = SceneIssueNumberRegex.Matches(name);
                for (var i = sceneMatches.Count - 1; i >= 0; i--)
                {
                    var sm = sceneMatches[i];
                    if (float.TryParse(sm.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var num) &&
                        num == parsed.IssueNumber.Value &&
                        sm.Index < cutoff)
                    {
                        cutoff = sm.Index;
                        break;
                    }
                }
            }

            // Check for limited series marker ("of N")
            var limitedMatch = LimitedSeriesRegex.Match(name);
            if (limitedMatch.Success && limitedMatch.Index < cutoff)
            {
                cutoff = limitedMatch.Index;
            }

            // Check for collected edition markers
            var collectedMatch = CollectedEditionRegex.Match(name);
            if (collectedMatch.Success && collectedMatch.Index < cutoff)
            {
                cutoff = collectedMatch.Index;
            }

            var stripped = name.Substring(0, cutoff).Trim();

            // Strip publisher prefix if present (e.g. "DC Comics - Batman" → "Batman")
            stripped = PublisherPrefixRegex.Replace(stripped, "");

            // Remove Annual/Special markers (keep them in IssueType, strip from title)
            stripped = AnnualRegex.Replace(stripped, " ").Trim();
            stripped = SpecialRegex.Replace(stripped, " ").Trim();

            // Remove bare years from the series title only if they match the extracted year
            // (e.g. "Batman 2016" -> "Batman" when year=2016, but keep "Spider-Man 2099")
            if (parsed.Year.HasValue)
            {
                stripped = Regex.Replace(stripped, $@"\b{parsed.Year.Value}\b", " ").Trim();
            }

            // Clean up extra spaces and trailing dashes/underscores
            stripped = Regex.Replace(stripped, @"\s{2,}", " ").Trim();
            stripped = stripped.TrimEnd('-', '_', ' ');

            return stripped.IsNullOrWhiteSpace() ? name : stripped;
        }

        // A release that bundles multiple issues: weekly 0-day packs, issue
        // ranges, volume ranges. Detection is advisory — the decision engine
        // uses it to keep packs out of automatic grabs; import never trusts
        // it and identifies files by their own content.
        private static bool IsMultiIssueName(string name)
        {
            if (PackKeywordRegex.IsMatch(name))
            {
                return true;
            }

            if (IssueRangeRegex.IsMatch(name))
            {
                return true;
            }

            var undated = FullDateRegex.Replace(name, " ");

            foreach (Match match in SceneRangeRegex.Matches(undated))
            {
                if (int.TryParse(match.Groups[1].Value, out var start) &&
                    int.TryParse(match.Groups[2].Value, out var end) &&
                    start < end)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
