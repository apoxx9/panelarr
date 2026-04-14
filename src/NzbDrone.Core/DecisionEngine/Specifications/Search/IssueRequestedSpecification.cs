using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.Search
{
    public class IssueRequestedSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public IssueRequestedSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public Decision IsSatisfiedBy(RemoteIssue remoteIssue, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria == null)
            {
                return Decision.Accept();
            }

            var criteriaIssue = searchCriteria.Issues.Select(v => v.Id).ToList();
            var remoteIssues = remoteIssue.Issues.Select(v => v.Id).ToList();

            if (!criteriaIssue.Intersect(remoteIssues).Any())
            {
                _logger.Debug("Release rejected since the issue wasn't requested: {0}", remoteIssue.ParsedIssueInfo);
                return Decision.Reject("Issue wasn't requested");
            }

            return Decision.Accept();
        }
    }
}
