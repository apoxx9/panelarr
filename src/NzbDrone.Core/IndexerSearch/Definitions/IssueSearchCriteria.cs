using NzbDrone.Core.Parser;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class IssueSearchCriteria : SearchCriteriaBase
    {
        public string IssueTitle { get; set; }
        public int IssueYear { get; set; }
        public string IssueIsbn { get; set; }
        public string Disambiguation { get; set; }

        public string IssueQuery => GetQueryTitle(IssueTitle.SplitBookTitle(Series.Name).Item1);

        public override string ToString()
        {
            return $"[{Series.Name} - {IssueTitle}]";
        }
    }
}
