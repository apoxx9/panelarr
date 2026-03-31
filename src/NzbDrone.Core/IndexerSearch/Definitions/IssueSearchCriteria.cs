using NzbDrone.Core.Parser;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class IssueSearchCriteria : SearchCriteriaBase
    {
        public string IssueTitle { get; set; }
        public int IssueYear { get; set; }
        public string IssueIsbn { get; set; }
        public string Disambiguation { get; set; }

        public string IssueQuery => GetQueryTitle((IssueTitle ?? string.Empty).SplitBookTitle(Series?.Name ?? string.Empty).Item1);

        public override string ToString()
        {
            return $"[{Series?.Name} - {IssueTitle ?? "Unknown"}]";
        }
    }
}
