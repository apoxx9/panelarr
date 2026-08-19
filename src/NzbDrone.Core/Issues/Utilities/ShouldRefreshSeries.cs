using System;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Issues
{
    public interface ICheckIfSeriesShouldBeRefreshed
    {
        bool ShouldRefresh(Series series);
    }

    // Tiered refresh cadence, sized for a library of a thousand-plus series
    // against ComicVine's ~200-requests/hour budget. Each refresh re-fetches
    // the series AND its issue list, so what matters is how many series the
    // daily pass lets through: ongoing series refresh daily, ended series
    // weekly, and only a recent release (the one signal that a "finished"
    // series is still moving) promotes an ended series to the daily tier.
    // An ended series that revives is caught by its next weekly pass - or
    // sooner, when an RSS release for it lands with no known issue.
    public class ShouldRefreshSeries : ICheckIfSeriesShouldBeRefreshed
    {
        public static readonly TimeSpan MinimumInterval = TimeSpan.FromHours(12);
        public static readonly TimeSpan ContinuingInterval = TimeSpan.FromDays(1);
        public static readonly TimeSpan EndedInterval = TimeSpan.FromDays(7);
        public static readonly TimeSpan RecentReleaseWindow = TimeSpan.FromDays(30);

        private readonly IIssueService _issueService;
        private readonly Logger _logger;

        public ShouldRefreshSeries(IIssueService issueService, Logger logger)
        {
            _issueService = issueService;
            _logger = logger;
        }

        public bool ShouldRefresh(Series series)
        {
            var now = DateTime.UtcNow;
            var sinceSync = now - series.LastInfoSync;

            if (sinceSync < MinimumInterval)
            {
                _logger.Trace("Series {0} last updated less than 12 hours ago, should not be refreshed.", series.Name);
                return false;
            }

            var status = series.Metadata.Value.Status;
            var isActive = status == SeriesStatusType.Continuing || status == SeriesStatusType.Hiatus;

            if (isActive)
            {
                if (sinceSync >= ContinuingInterval)
                {
                    _logger.Trace("Series {0} is continuing and has not been refreshed in a day, should refresh.", series.Name);
                    return true;
                }

                _logger.Trace("Series {0} is continuing and was refreshed within a day, should not be refreshed.", series.Name);
                return false;
            }

            if (sinceSync >= EndedInterval)
            {
                _logger.Trace("Series {0} is ended and has not been refreshed in a week, should refresh.", series.Name);
                return true;
            }

            // An "ended" series with an issue released in the last month is
            // either mis-flagged or getting a late entry - treat it as active
            var lastIssue = _issueService.GetIssuesBySeries(series.Id).MaxBy(e => e.ReleaseDate);

            if (lastIssue != null && lastIssue.ReleaseDate > now - RecentReleaseWindow && sinceSync >= ContinuingInterval)
            {
                _logger.Trace("Last issue in ended series {0} released less than 30 days ago, should refresh daily.", series.Name);
                return true;
            }

            _logger.Trace("Series {0} is ended and was refreshed within a week, should not be refreshed.", series.Name);
            return false;
        }
    }
}
