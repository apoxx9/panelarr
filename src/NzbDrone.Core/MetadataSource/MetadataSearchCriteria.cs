using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.MetadataSource
{
    public class MetadataSearchCriteria
    {
        // Match trailing 4-digit year with whitespace before it
        private static readonly Regex TrailingYearRegex = new Regex(
            @"\s+(\d{4})\s*$",
            RegexOptions.Compiled);

        // 4-digit numbers that are known comic series names, not publication years
        // Only includes numbers outside the plausible year range (handled by the heuristic below)
        private static readonly HashSet<string> FuturisticSeriesNumbers = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "2099", "1602", "3000"
        };

        public string Term { get; set; }
        public int? Year { get; set; }

        public MetadataSearchCriteria(string term, int? year = null)
        {
            var rawTerm = term?.Trim() ?? string.Empty;

            // If no explicit year provided, try to extract a trailing 4-digit year
            // e.g. "superman 2026" -> Term="superman", Year=2026
            if (!year.HasValue && rawTerm.Length > 0)
            {
                var match = TrailingYearRegex.Match(rawTerm);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var parsedYear) && parsedYear >= 1900 && parsedYear <= 2100)
                {
                    var termWithoutYear = rawTerm.Substring(0, match.Index).Trim();

                    // Don't strip the year if:
                    // 1. It would leave the term empty (the year IS the series name, e.g. "1602")
                    // 2. The number is part of a known series name pattern (e.g. "x-men 2099", "judge dredd 2000")
                    if (termWithoutYear.Length > 0 && !LooksLikePartOfSeriesName(termWithoutYear, match.Groups[1].Value))
                    {
                        year = parsedYear;
                        rawTerm = termWithoutYear;
                    }
                }
            }

            Term = rawTerm;
            Year = year;
        }

        private static bool LooksLikePartOfSeriesName(string termBeforeYear, string yearStr)
        {
            // If the term before the year ends with a common series-year connector, it's part of the name
            // e.g. "x-men" + "2099" -> "x-men 2099" is a series name, not a year filter
            // But "superman" + "2026" -> "superman 2026" is a year filter
            if (FuturisticSeriesNumbers.Contains(yearStr))
            {
                // Only treat as series name if the preceding word connects to a known pattern
                // "x-men 2099" -> yes (2099 is in the list)
                // "batman 2024" -> no, because 2024 is a plausible year filter
                // Heuristic: if the year is > current year or < 1940, it's probably a series name
                if (int.TryParse(yearStr, out var y))
                {
                    var currentYear = System.DateTime.UtcNow.Year;
                    if (y > currentYear + 5 || y < 1940)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
