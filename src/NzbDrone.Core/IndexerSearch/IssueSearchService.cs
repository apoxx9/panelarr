using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Queue;

namespace NzbDrone.Core.IndexerSearch
{
    internal class IssueSearchService : IExecute<IssueSearchCommand>,
                               IExecute<MissingIssueSearchCommand>,
                               IExecute<CutoffUnmetIssueSearchCommand>
    {
        private readonly ISearchForReleases _releaseSearchService;
        private readonly IIssueService _issueService;
        private readonly IIssueCutoffService _issueCutoffService;
        private readonly IQueueService _queueService;
        private readonly IProcessDownloadDecisions _processDownloadDecisions;
        private readonly Logger _logger;

        public IssueSearchService(ISearchForReleases releaseSearchService,
            IIssueService issueService,
            IIssueCutoffService issueCutoffService,
            IQueueService queueService,
            IProcessDownloadDecisions processDownloadDecisions,
            Logger logger)
        {
            _releaseSearchService = releaseSearchService;
            _issueService = issueService;
            _issueCutoffService = issueCutoffService;
            _queueService = queueService;
            _processDownloadDecisions = processDownloadDecisions;
            _logger = logger;
        }

        private async Task SearchForBulkIssues(List<Issue> issues, bool userInvokedSearch)
        {
            _logger.ProgressInfo("Performing missing search for {0} issues", issues.Count);
            var downloadedCount = 0;

            foreach (var issue in issues.OrderBy(a => a.LastSearchTime ?? DateTime.MinValue))
            {
                List<DownloadDecision> decisions;

                try
                {
                    decisions = await _releaseSearchService.IssueSearch(issue.Id, false, userInvokedSearch, false);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unable to search for issue: [{0}]", issue);
                    continue;
                }

                var processed = await _processDownloadDecisions.ProcessDecisions(decisions);

                downloadedCount += processed.Grabbed.Count;
            }

            _logger.ProgressInfo("Completed search for {0} issues. {1} reports downloaded.", issues.Count, downloadedCount);
        }

        public void Execute(IssueSearchCommand message)
        {
            foreach (var issueId in message.IssueIds)
            {
                var decisions = _releaseSearchService.IssueSearch(issueId, false, message.Trigger == CommandTrigger.Manual, false).GetAwaiter().GetResult();
                var processed = _processDownloadDecisions.ProcessDecisions(decisions).GetAwaiter().GetResult();

                _logger.ProgressInfo("Issue search completed. {0} reports downloaded.", processed.Grabbed.Count);
            }
        }

        public void Execute(MissingIssueSearchCommand message)
        {
            List<Issue> issues;

            if (message.SeriesId.HasValue)
            {
                var seriesId = message.SeriesId.Value;

                var pagingSpec = new PagingSpec<Issue>
                {
                    Page = 1,
                    PageSize = 100000,
                    SortDirection = SortDirection.Ascending,
                    SortKey = "Id"
                };

                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Series.Value.Monitored == true);

                issues = _issueService.IssuesWithoutFiles(pagingSpec).Records.Where(e => e.SeriesId.Equals(seriesId)).ToList();
            }
            else
            {
                var pagingSpec = new PagingSpec<Issue>
                {
                    Page = 1,
                    PageSize = 100000,
                    SortDirection = SortDirection.Ascending,
                    SortKey = "Id"
                };

                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Series.Value.Monitored == true);

                issues = _issueService.IssuesWithoutFiles(pagingSpec).Records.ToList();
            }

            var queue = _queueService.GetQueue().Where(q => q.Issue != null).Select(q => q.Issue.Id);
            var missing = issues.Where(e => !queue.Contains(e.Id)).ToList();

            SearchForBulkIssues(missing, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
        }

        public void Execute(CutoffUnmetIssueSearchCommand message)
        {
            var pagingSpec = new PagingSpec<Issue>
            {
                Page = 1,
                PageSize = 100000,
                SortDirection = SortDirection.Ascending,
                SortKey = "Id"
            };

            pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Series.Value.Monitored == true);

            var issues = _issueCutoffService.IssuesWhereCutoffUnmet(pagingSpec).Records.ToList();

            var queue = _queueService.GetQueue().Where(q => q.Issue != null).Select(q => q.Issue.Id);
            var cutoffUnmet = issues.Where(e => !queue.Contains(e.Id)).ToList();

            SearchForBulkIssues(cutoffUnmet, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
        }
    }
}
