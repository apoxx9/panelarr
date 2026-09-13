using System;
using System.Collections.Generic;
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
    // monthly, and only a recent release (the one signal that a "finished"
    // series is still moving) promotes an ended series to the daily tier.
    // An ended series that revives is caught sooner than its monthly pass:
    // an RSS release naming an unknown issue triggers an on-demand refresh,
    // and the recent release then holds it in the daily tier.
    //
    // The ended tier shares ONE interval, so series stamped on the same day
    // come due together forever - the whole tier once converged on a single
    // date and produced an 11h30m weekly run. BudgetEndedTier caps the tier
    // at its steady-state arrival rate (tier size / interval days), oldest
    // due first, which disperses any such bunching within one interval.
    public class ShouldRefreshSeries : ICheckIfSeriesShouldBeRefreshed
    {
        public static readonly TimeSpan MinimumInterval = TimeSpan.FromHours(12);
        public static readonly TimeSpan ContinuingInterval = TimeSpan.FromDays(1);
        public static readonly TimeSpan EndedInterval = TimeSpan.FromDays(30);
        public static readonly TimeSpan RecentReleaseWindow = TimeSpan.FromDays(30);

        private readonly IIssueService _issueService;
        private readonly Logger _logger;

        public ShouldRefreshSeries(IIssueService issueService, Logger logger)
        {
            _issueService = issueService;
            _logger = logger;
        }

        public static bool IsEndedTier(Series series)
        {
            var status = series.Metadata.Value.Status;

            return status != SeriesStatusType.Continuing && status != SeriesStatusType.Hiatus;
        }

        public static bool IsEndedTierDue(Series series)
        {
            return IsEndedTier(series) && DateTime.UtcNow - series.LastInfoSync >= EndedInterval;
        }

        // The scheduled refresh's daily allowance for the ended tier: enough
        // to cycle the whole tier once per interval, no more - so a bunched
        // tier drains evenly instead of in one storm. Active series are not
        // this budget's concern and revival promotions bypass it.
        public static HashSet<int> BudgetEndedTier(List<Series> allSeries)
        {
            var tier = allSeries.Where(IsEndedTier).ToList();

            if (!tier.Any())
            {
                return new HashSet<int>();
            }

            var budget = Math.Max(1, (int)Math.Ceiling(tier.Count / EndedInterval.TotalDays));

            return tier.Where(IsEndedTierDue)
                       .OrderBy(x => x.LastInfoSync)
                       .Take(budget)
                       .Select(x => x.Id)
                       .ToHashSet();
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

            var isActive = !IsEndedTier(series);

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
                _logger.Trace("Series {0} is ended and has not been refreshed within the interval, should refresh.", series.Name);
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

            _logger.Trace("Series {0} is ended and was refreshed within the interval, should not be refreshed.", series.Name);
            return false;
        }
    }
}
