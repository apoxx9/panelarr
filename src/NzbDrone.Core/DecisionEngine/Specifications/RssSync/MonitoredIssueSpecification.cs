using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications.RssSync
{
    public class MonitoredIssueSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public MonitoredIssueSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteIssue subject, SearchCriteriaBase searchCriteria)
        {
            if (searchCriteria != null)
            {
                if (!searchCriteria.MonitoredIssuesOnly)
                {
                    _logger.Debug("Skipping monitored check during search");
                    return Decision.Accept();
                }
            }

            if (!subject.Series.Monitored)
            {
                _logger.Debug("{0} is present in the DB but not tracked. Rejecting.", subject.Series);
                return Decision.Reject("Series is not monitored");
            }

            var monitoredCount = subject.Issues.Count(issue => issue.Monitored);
            if (monitoredCount == subject.Issues.Count)
            {
                return Decision.Accept();
            }

            if (subject.Issues.Count == 1)
            {
                _logger.Debug("Issue is not monitored. Rejecting", monitoredCount, subject.Issues.Count);
                return Decision.Reject("Issue is not monitored");
            }

            if (monitoredCount == 0)
            {
                _logger.Debug("No issues in the release are monitored. Rejecting", monitoredCount, subject.Issues.Count);
            }
            else
            {
                _logger.Debug("Only {0}/{1} issues in the release are monitored. Rejecting", monitoredCount, subject.Issues.Count);
            }

            return Decision.Reject("Issue is not monitored");
        }
    }
}
