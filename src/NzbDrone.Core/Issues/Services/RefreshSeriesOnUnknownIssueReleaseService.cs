using System;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    // The safety valve for the tiered refresh cadence: an ended series is
    // only refreshed weekly, but when an RSS release matches the series and
    // names no issue Panelarr knows, something is clearly happening to it -
    // a revival, a late entry, a CV listing added since the last refresh.
    // Refresh that series now rather than up to a week from now. Throttled
    // per series so a release nobody can resolve cannot hammer ComicVine
    // every RSS cycle.
    public class RefreshSeriesOnUnknownIssueReleaseService : IHandle<RssSyncCompleteEvent>
    {
        public static readonly TimeSpan PerSeriesThrottle = TimeSpan.FromHours(12);

        private readonly IManageCommandQueue _commandQueueManager;
        private readonly ICached<DateTime> _lastTriggered;
        private readonly Logger _logger;

        public RefreshSeriesOnUnknownIssueReleaseService(IManageCommandQueue commandQueueManager,
                                                         ICacheManager cacheManager,
                                                         Logger logger)
        {
            _commandQueueManager = commandQueueManager;
            _lastTriggered = cacheManager.GetCache<DateTime>(GetType());
            _logger = logger;
        }

        public void Handle(RssSyncCompleteEvent message)
        {
            var rejected = message.ProcessedDecisions?.Rejected;

            if (rejected == null || !rejected.Any())
            {
                return;
            }

            var seriesToRefresh = rejected
                .Select(d => d.RemoteIssue)
                .Where(r => r?.Series != null && (r.Issues == null || !r.Issues.Any()))
                .Select(r => r.Series)
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .ToList();

            foreach (var series in seriesToRefresh)
            {
                var key = series.Id.ToString();
                var last = _lastTriggered.Find(key);

                if (last != default && DateTime.UtcNow - last < PerSeriesThrottle)
                {
                    continue;
                }

                _lastTriggered.Set(key, DateTime.UtcNow, PerSeriesThrottle);
                _logger.Info("RSS release matched {0} but no known issue - refreshing series info", series.Name);
                _commandQueueManager.Push(new RefreshSeriesCommand(series.Id));
            }
        }
    }
}
