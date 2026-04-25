using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface IIssueAddedService
    {
        void SearchForRecentlyAdded(int seriesId);
    }

    public class IssueAddedService : IHandle<IssueInfoRefreshedEvent>, IIssueAddedService
    {
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IIssueService _issueService;
        private readonly Logger _logger;
        private readonly ICached<List<int>> _addedIssuesCache;

        public IssueAddedService(ICacheManager cacheManager,
                                   IManageCommandQueue commandQueueManager,
                                   IIssueService issueService,
                                   Logger logger)
        {
            _commandQueueManager = commandQueueManager;
            _issueService = issueService;
            _logger = logger;
            _addedIssuesCache = cacheManager.GetCache<List<int>>(GetType());
        }

        public void SearchForRecentlyAdded(int seriesId)
        {
            var allIssues = _issueService.GetIssuesBySeries(seriesId);
            var toSearch = allIssues.Where(x => x.AddOptions.SearchForNewIssue).ToList();

            if (toSearch.Any())
            {
                toSearch.ForEach(x => x.AddOptions.SearchForNewIssue = false);

                _issueService.SetAddOptions(toSearch);
            }

            var recentlyAddedIds = _addedIssuesCache.Find(seriesId.ToString());
            if (recentlyAddedIds != null)
            {
                toSearch.AddRange(allIssues.Where(x => recentlyAddedIds.Contains(x.Id)));
            }

            if (toSearch.Any())
            {
                _commandQueueManager.Push(new IssueSearchCommand(toSearch.Select(e => e.Id).ToList()));
            }

            _addedIssuesCache.Remove(seriesId.ToString());
        }

        public void Handle(IssueInfoRefreshedEvent message)
        {
            if (message.Series.AddOptions == null)
            {
                if (!message.Series.Monitored)
                {
                    _logger.Debug("Series is not monitored");
                    return;
                }

                if (message.Added.Empty())
                {
                    _logger.Debug("No new issues, skipping search");
                    return;
                }

                if (message.Added.None(a => a.ReleaseDate.HasValue))
                {
                    _logger.Debug("No new issues have an release date");
                    return;
                }

                var previouslyReleased = message.Added.Where(a => a.ReleaseDate.HasValue && a.ReleaseDate.Value.Before(DateTime.UtcNow.AddDays(1)) && a.Monitored).ToList();

                if (previouslyReleased.Empty())
                {
                    _logger.Debug("Newly added issues all release in the future");
                    return;
                }

                _addedIssuesCache.Set(message.Series.Id.ToString(), previouslyReleased.Select(e => e.Id).ToList());
            }
        }
    }
}
