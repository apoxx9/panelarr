using System;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Issues
{
    public interface ICheckIfSeriesShouldBeRefreshed
    {
        bool ShouldRefresh(Series series);
    }

    public class ShouldRefreshSeries : ICheckIfSeriesShouldBeRefreshed
    {
        private readonly IIssueService _issueService;
        private readonly Logger _logger;

        public ShouldRefreshSeries(IIssueService issueService, Logger logger)
        {
            _issueService = issueService;
            _logger = logger;
        }

        public bool ShouldRefresh(Series series)
        {
            if (series.LastInfoSync < DateTime.UtcNow.AddDays(-30))
            {
                _logger.Trace("Series {0} last updated more than 30 days ago, should refresh.", series.Name);
                return true;
            }

            if (series.LastInfoSync >= DateTime.UtcNow.AddHours(-12))
            {
                _logger.Trace("Series {0} last updated less than 12 hours ago, should not be refreshed.", series.Name);
                return false;
            }

            if (series.Metadata.Value.Status == SeriesStatusType.Continuing && series.LastInfoSync < DateTime.UtcNow.AddDays(-2))
            {
                _logger.Trace("Series {0} is continuing and has not been refreshed in 2 days, should refresh.", series.Name);
                return true;
            }

            var lastIssue = _issueService.GetIssuesBySeries(series.Id).MaxBy(e => e.ReleaseDate);

            if (lastIssue != null && lastIssue.ReleaseDate > DateTime.UtcNow.AddDays(-30))
            {
                _logger.Trace("Last issue in {0} released less than 30 days ago, should refresh.", series.Name);
                return true;
            }

            _logger.Trace("Series {0} ended long ago, should not be refreshed.", series.Name);
            return false;
        }
    }
}
