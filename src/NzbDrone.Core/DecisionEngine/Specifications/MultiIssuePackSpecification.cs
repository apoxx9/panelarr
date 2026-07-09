using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class MultiIssuePackSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public MultiIssuePackSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        // Packs bundle many issues behind a single-issue mapping, so an
        // automatic grab would fetch gigabytes for one wanted file and import
        // the rest unvetted. The user can still grab one deliberately from
        // interactive search; import identifies each contained file on its own.
        public virtual Decision IsSatisfiedBy(RemoteIssue subject, SearchCriteriaBase searchCriteria)
        {
            if (subject.ParsedIssueInfo?.IsMultiIssue != true)
            {
                return Decision.Accept();
            }

            if (searchCriteria?.InteractiveSearch == true)
            {
                _logger.Debug("[{0}] is a multi-issue pack, allowing in interactive search", subject.Release.Title);
                return Decision.Accept();
            }

            _logger.Debug("[{0}] is a multi-issue pack, rejecting automatic grab", subject.Release.Title);
            return Decision.Reject("Multi-issue pack: automatic grabbing is disabled, grab manually from interactive search");
        }
    }
}
