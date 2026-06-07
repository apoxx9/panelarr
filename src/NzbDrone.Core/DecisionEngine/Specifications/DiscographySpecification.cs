using System;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class DiscographySpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public DiscographySpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteIssue subject, SearchCriteriaBase searchCriteria)
        {
            if (subject.ParsedIssueInfo.Discography)
            {
                _logger.Debug("Checking if all issues in collection release have released. {0}", subject.Release.Title);

                if (subject.Issues.Any(e => !e.ReleaseDate.HasValue || e.ReleaseDate.Value.After(DateTime.UtcNow)))
                {
                    _logger.Debug("Collection release {0} rejected. All issues haven't released yet.", subject.Release.Title);
                    return Decision.Reject("Collection release rejected. All issues haven't released yet.");
                }
            }

            return Decision.Accept();
        }
    }
}
