using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class SingleIssueSearchMatchSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public SingleIssueSearchMatchSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteIssue remoteIssue, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria == null)
            {
                return Decision.Accept();
            }

            var singleIssueSpec = searchCriteria as IssueSearchCriteria;
            if (singleIssueSpec == null)
            {
                return Decision.Accept();
            }

            if (!remoteIssue.ParsedIssueInfo.IssueTitle.Any())
            {
                _logger.Debug("Full discography result during single issue search, skipping.");
                return Decision.Reject("Full series pack");
            }

            return Decision.Accept();
        }
    }
}
