using System.Text.RegularExpressions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class IssueSearchCriteria : SearchCriteriaBase
    {
        // Generic volume boilerplate CV puts on collection issues ("Volume 1",
        // "Book One", "Part 2") — worthless as search terms: AND-matching
        // trackers require every word, so they exclude the real releases.
        private static readonly Regex BoilerplateTitleRegex = new Regex(
            @"^(?:volume|vol\.?|book|part|tome)\s*(?:\d+|one|two|three|four|five)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string IssueTitle { get; set; }
        public float IssueNumber { get; set; }
        public int IssueYear { get; set; }
        public string IssueIsbn { get; set; }
        public string Disambiguation { get; set; }

        // A series with exactly one issue is a book: its searchable identity
        // is the series name alone, whatever CV titled the issue.
        public bool SingleIssueSeries { get; set; }

        public string IssueQuery
        {
            get
            {
                if (SingleIssueSeries)
                {
                    return string.Empty;
                }

                var title = IssueTitle ?? string.Empty;

                if (BoilerplateTitleRegex.IsMatch(title.Trim()))
                {
                    title = string.Empty;
                }

                // For comics: use issue number when title is empty
                if (string.IsNullOrWhiteSpace(title) && IssueNumber > 0)
                {
                    return $"#{(int)IssueNumber}";
                }

                return GetQueryTitle(title.SplitIssueTitle(Series?.Name ?? string.Empty).Item1);
            }
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(IssueTitle) && IssueNumber > 0)
            {
                return $"[{Series?.Name} #{(int)IssueNumber}]";
            }

            return $"[{Series?.Name} - {IssueTitle ?? "Unknown"}]";
        }
    }
}
