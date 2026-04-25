using NzbDrone.Core.Parser;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class IssueSearchCriteria : SearchCriteriaBase
    {
        public string IssueTitle { get; set; }
        public float IssueNumber { get; set; }
        public int IssueYear { get; set; }
        public string IssueIsbn { get; set; }
        public string Disambiguation { get; set; }

        public string IssueQuery
        {
            get
            {
                // For comics: use issue number when title is empty
                if (string.IsNullOrWhiteSpace(IssueTitle) && IssueNumber > 0)
                {
                    return $"#{(int)IssueNumber}";
                }

                return GetQueryTitle((IssueTitle ?? string.Empty).SplitIssueTitle(Series?.Name ?? string.Empty).Item1);
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
