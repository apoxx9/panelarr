using System.IO;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
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

        // Year in parentheses: (2022)
        private static readonly Regex YearRegex = new Regex(
            @"\((\d{4})\)",
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
            @"\b(?:TPB|Trade\s*Paperback|HC|Hardcover|Omnibus|One.?Shot)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Source detection
        private static readonly Regex SourceRegex = new Regex(
            @"\b(?:Digital|Print|Scan|c2c|noads)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Release group (typically last parenthetical containing known group indicators)
        private static readonly Regex ReleaseGroupRegex = new Regex(
            @"\(([^)]*(?:Empire|Minutemen|DCP|Mephisto|Digi|GetComics|Zone|Shan|GreenGiant|Novus|Senpai)(?:[^)]*)?)\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Generic last parenthetical for release group fallback
        private static readonly Regex LastParenthetical = new Regex(
            @"\(([^)]+)\)\s*$",
            RegexOptions.Compiled);

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

            // Strip extension for parsing
            var name = result.Format != ComicFormat.Unknown
                ? Path.GetFileNameWithoutExtension(title)
                : title;

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
                else if (token.Contains("omnibus"))
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
                if (issueMatch.Success && float.TryParse(issueMatch.Groups[1].Value, out var issueNum))
                {
                    result.IssueNumber = issueNum;
                }
                else
                {
                    // Try scene-style 3-digit number
                    var sceneMatch = SceneIssueNumberRegex.Match(name);
                    if (sceneMatch.Success && float.TryParse(sceneMatch.Groups[1].Value, out var sceneNum))
                    {
                        result.IssueNumber = sceneNum;
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

            // Extract series title — everything before the first issue/volume marker or first parenthetical
            result.SeriesTitle = ExtractSeriesTitle(name, result);

            // Set quality based on format
            result.Quality = new QualityModel(MapFormatToQuality(result.Format));

            return result;
        }

        private static string ExtractSeriesTitle(string name, ParsedComicInfo parsed)
        {
            // Remove all parentheticals first
            var stripped = Regex.Replace(name, @"\([^)]*\)", " ").Trim();

            // Remove volume indicators
            stripped = VolumeRegex.Replace(stripped, " ").Trim();

            // Remove issue number patterns
            stripped = IssueNumberRegex.Replace(stripped, " ").Trim();
            stripped = SceneIssueNumberRegex.Replace(stripped, " ").Trim();

            // Remove collected edition markers
            stripped = CollectedEditionRegex.Replace(stripped, " ").Trim();

            // Remove Annual/Special/Annual markers (keep them in IssueType, strip from title)
            stripped = AnnualRegex.Replace(stripped, " ").Trim();
            stripped = SpecialRegex.Replace(stripped, " ").Trim();

            // Remove source
            stripped = SourceRegex.Replace(stripped, " ").Trim();

            // Clean up extra spaces and trailing dashes/underscores
            stripped = Regex.Replace(stripped, @"\s{2,}", " ").Trim();
            stripped = stripped.TrimEnd('-', '_', ' ');

            return stripped.IsNullOrWhiteSpace() ? name : stripped;
        }

        private static Quality MapFormatToQuality(ComicFormat format)
        {
            return format switch
            {
                ComicFormat.CBZ => Quality.CBZ,
                ComicFormat.CBR => Quality.CBR,
                ComicFormat.CB7 => Quality.CB7,
                ComicFormat.PDF => Quality.PDF,
                ComicFormat.EPUB => Quality.EPUB,
                _ => Quality.Unknown
            };
        }
    }
}
