using System;
using System.Linq;
using NLog;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    // Release names carry a year ("Green Lantern Corps #18 (2026)") that is
    // either the issue's cover year or the series' start year, depending on
    // the group's naming convention. Name-based mapping ignores it, so a new
    // release of a current series can map onto an old same-named series whose
    // issue numbers overlap. The import pipeline scores the year and refuses
    // such files at 80%; this applies the same signal at grab time so the
    // wrong release is never downloaded at all.
    public class YearMatchSpecification : IDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public YearMatchSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public virtual Decision IsSatisfiedBy(RemoteIssue subject, SearchCriteriaBase searchCriteria)
        {
            var parsedYear = subject.ParsedIssueInfo?.SeriesTitleInfo?.Year ?? 0;

            if (parsedYear == 0)
            {
                return Decision.Accept();
            }

            var seriesYear = subject.Series?.Metadata?.Value?.Year ?? 0;

            if (seriesYear == parsedYear)
            {
                return Decision.Accept();
            }

            var issueYears = subject.Issues
                .Where(i => i.ReleaseDate.HasValue)
                .Select(i => i.ReleaseDate.Value.Year)
                .Distinct()
                .ToList();

            if (issueYears.Any(y => Math.Abs(y - parsedYear) <= 1))
            {
                return Decision.Accept();
            }

            // Reject only on positive evidence of a mismatch: with no issue
            // dates and no series year there is nothing to compare against.
            if (!issueYears.Any() && seriesYear == 0)
            {
                return Decision.Accept();
            }

            var known = issueYears.Any()
                ? string.Join("/", issueYears.OrderBy(y => y))
                : seriesYear.ToString();

            _logger.Debug("[{0}] Release year {1} does not match issue year(s) {2} or series year {3}",
                          subject.Release.Title,
                          parsedYear,
                          string.Join("/", issueYears),
                          seriesYear);

            return Decision.Reject("Release year {0} does not match issue year {1}", parsedYear, known);
        }
    }
}
