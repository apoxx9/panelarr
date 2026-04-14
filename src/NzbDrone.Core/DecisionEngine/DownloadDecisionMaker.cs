using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download.Aggregation;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine
{
    public interface IMakeDownloadDecision
    {
        List<DownloadDecision> GetRssDecision(List<ReleaseInfo> reports, bool pushedRelease = false);
        List<DownloadDecision> GetSearchDecision(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteriaBase);
    }

    public class DownloadDecisionMaker : IMakeDownloadDecision
    {
        private readonly IEnumerable<IDecisionEngineSpecification> _specifications;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IParsingService _parsingService;
        private readonly IRemoteIssueAggregationService _aggregationService;
        private readonly Logger _logger;

        public DownloadDecisionMaker(IEnumerable<IDecisionEngineSpecification> specifications,
            IParsingService parsingService,
            ICustomFormatCalculationService formatService,
            IRemoteIssueAggregationService aggregationService,
            Logger logger)
        {
            _specifications = specifications;
            _parsingService = parsingService;
            _formatCalculator = formatService;
            _aggregationService = aggregationService;
            _logger = logger;
        }

        public List<DownloadDecision> GetRssDecision(List<ReleaseInfo> reports, bool pushedRelease = false)
        {
            return GetIssueDecisions(reports).ToList();
        }

        public List<DownloadDecision> GetSearchDecision(List<ReleaseInfo> reports, SearchCriteriaBase searchCriteriaBase)
        {
            return GetIssueDecisions(reports, false, searchCriteriaBase).ToList();
        }

        private IEnumerable<DownloadDecision> GetIssueDecisions(List<ReleaseInfo> reports, bool pushedRelease = false, SearchCriteriaBase searchCriteria = null)
        {
            if (reports.Any())
            {
                _logger.ProgressInfo("Processing {0} releases", reports.Count);
            }
            else
            {
                _logger.ProgressInfo("No results found");
            }

            var reportNumber = 1;

            foreach (var report in reports)
            {
                DownloadDecision decision = null;
                _logger.ProgressTrace("Processing release {0}/{1}", reportNumber, reports.Count);
                _logger.Debug("Processing release '{0}' from '{1}'", report.Title, report.Indexer);

                try
                {
                    var parsedIssueInfo = Parser.Parser.ParseIssueTitle(report.Title);

                    if (parsedIssueInfo == null)
                    {
                        if (searchCriteria != null)
                        {
                            parsedIssueInfo = Parser.Parser.ParseIssueTitleWithSearchCriteria(report.Title,
                                                                                              searchCriteria.Series,
                                                                                              searchCriteria.Issues);
                        }
                        else
                        {
                            // try parsing fuzzy
                            parsedIssueInfo = _parsingService.ParseIssueTitleFuzzy(report.Title);
                        }
                    }

                    if (parsedIssueInfo != null && !parsedIssueInfo.SeriesName.IsNullOrWhiteSpace())
                    {
                        var remoteIssue = _parsingService.Map(parsedIssueInfo, searchCriteria);
                        remoteIssue.Release = report;

                        _aggregationService.Augment(remoteIssue);

                        // try parsing again using the search criteria, in case it parsed but parsed incorrectly
                        if ((remoteIssue.Series == null || remoteIssue.Issues.Empty()) && searchCriteria != null)
                        {
                            _logger.Debug("Series/Issue null for {0}, reparsing with search criteria", report.Title);
                            var parsedIssueInfoWithCriteria = Parser.Parser.ParseIssueTitleWithSearchCriteria(report.Title,
                                                                                                                searchCriteria.Series,
                                                                                                                searchCriteria.Issues);

                            if (parsedIssueInfoWithCriteria != null && parsedIssueInfoWithCriteria.SeriesName.IsNotNullOrWhiteSpace())
                            {
                                remoteIssue = _parsingService.Map(parsedIssueInfoWithCriteria, searchCriteria);
                            }
                        }

                        remoteIssue.Release = report;

                        // parse quality again with title and category if unknown
                        if (remoteIssue.ParsedIssueInfo.Quality.Quality == Quality.Unknown)
                        {
                            remoteIssue.ParsedIssueInfo.Quality = QualityParser.ParseQuality(report.Title, null, report.Categories);
                        }

                        if (remoteIssue.Series == null)
                        {
                            decision = new DownloadDecision(remoteIssue, new Rejection("Unknown Series"));

                            // shove in the searched series in case of forced download in interactive search
                            if (searchCriteria != null)
                            {
                                remoteIssue.Series = searchCriteria.Series;
                                remoteIssue.Issues = searchCriteria.Issues;
                            }
                        }
                        else if (remoteIssue.Issues.Empty())
                        {
                            if (searchCriteria != null)
                            {
                                // For interactive search, use the searched issues directly
                                remoteIssue.Issues = searchCriteria.Issues;
                            }
                            else
                            {
                                decision = new DownloadDecision(remoteIssue, new Rejection("Unable to parse issues from release name"));
                            }
                        }
                        else
                        {
                            _aggregationService.Augment(remoteIssue);

                            remoteIssue.CustomFormats = _formatCalculator.ParseCustomFormat(remoteIssue, remoteIssue.Release.Size);
                            remoteIssue.CustomFormatScore = remoteIssue?.Series?.QualityProfile?.Value.CalculateCustomFormatScore(remoteIssue.CustomFormats) ?? 0;

                            remoteIssue.DownloadAllowed = remoteIssue.Issues.Any();
                            decision = GetDecisionForReport(remoteIssue, searchCriteria);
                        }
                    }

                    if (searchCriteria != null)
                    {
                        if (parsedIssueInfo == null)
                        {
                            parsedIssueInfo = new ParsedIssueInfo
                            {
                                Quality = QualityParser.ParseQuality(report.Title, null, report.Categories)
                            };
                        }

                        if (parsedIssueInfo.SeriesName.IsNullOrWhiteSpace())
                        {
                            var remoteIssue = new RemoteIssue
                            {
                                Release = report,
                                ParsedIssueInfo = parsedIssueInfo
                            };

                            decision = new DownloadDecision(remoteIssue, new Rejection("Unable to parse release"));
                        }
                    }

                    if (searchCriteria != null)
                    {
                        if (parsedIssueInfo == null)
                        {
                            parsedIssueInfo = new ParsedIssueInfo
                            {
                                Quality = QualityParser.ParseQuality(report.Title, null, report.Categories)
                            };
                        }

                        if (parsedIssueInfo.SeriesName.IsNullOrWhiteSpace())
                        {
                            var remoteIssue = new RemoteIssue
                            {
                                Release = report,
                                ParsedIssueInfo = parsedIssueInfo
                            };

                            decision = new DownloadDecision(remoteIssue, new Rejection("Unable to parse release"));
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't process release.");

                    var remoteIssue = new RemoteIssue { Release = report };
                    decision = new DownloadDecision(remoteIssue, new Rejection("Unexpected error processing release"));
                }

                reportNumber++;

                if (decision != null)
                {
                    var source = pushedRelease ? ReleaseSourceType.ReleasePush : ReleaseSourceType.Rss;

                    if (searchCriteria != null)
                    {
                        if (searchCriteria.InteractiveSearch)
                        {
                            source = ReleaseSourceType.InteractiveSearch;
                        }
                        else if (searchCriteria.UserInvokedSearch)
                        {
                            source = ReleaseSourceType.UserInvokedSearch;
                        }
                        else
                        {
                            source = ReleaseSourceType.Search;
                        }
                    }

                    decision.RemoteIssue.ReleaseSource = source;

                    if (decision.Rejections.Any())
                    {
                        _logger.Debug("Release rejected for the following reasons: {0}", string.Join(", ", decision.Rejections));
                    }
                    else
                    {
                        _logger.Debug("Release accepted");
                    }

                    yield return decision;
                }
            }
        }

        private DownloadDecision GetDecisionForReport(RemoteIssue remoteIssue, SearchCriteriaBase searchCriteria = null)
        {
            var reasons = new Rejection[0];

            foreach (var specifications in _specifications.GroupBy(v => v.Priority).OrderBy(v => v.Key))
            {
                reasons = specifications.Select(c => EvaluateSpec(c, remoteIssue, searchCriteria))
                                                        .Where(c => c != null)
                                                        .ToArray();

                if (reasons.Any())
                {
                    break;
                }
            }

            return new DownloadDecision(remoteIssue, reasons.ToArray());
        }

        private Rejection EvaluateSpec(IDecisionEngineSpecification spec, RemoteIssue remoteIssue, SearchCriteriaBase searchCriteriaBase = null)
        {
            try
            {
                var result = spec.IsSatisfiedBy(remoteIssue, searchCriteriaBase);

                if (!result.Accepted)
                {
                    return new Rejection(result.Reason, spec.Type);
                }
            }
            catch (NotImplementedException)
            {
                _logger.Trace("Spec " + spec.GetType().Name + " not implemented.");
            }
            catch (Exception e)
            {
                e.Data.Add("report", remoteIssue.Release.ToJson());
                e.Data.Add("parsed", remoteIssue.ParsedIssueInfo.ToJson());
                _logger.Error(e, "Couldn't evaluate decision on {0}", remoteIssue.Release.Title);
                return new Rejection($"{spec.GetType().Name}: {e.Message}");
            }

            return null;
        }
    }
}
