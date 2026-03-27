using System;
using NLog;

namespace NzbDrone.Core.Books
{
    public interface ICheckIfBookShouldBeRefreshed
    {
        bool ShouldRefresh(Issue issue);
    }

    public class ShouldRefreshBook : ICheckIfBookShouldBeRefreshed
    {
        private readonly Logger _logger;

        public ShouldRefreshBook(Logger logger)
        {
            _logger = logger;
        }

        public bool ShouldRefresh(Issue issue)
        {
            if (issue.LastInfoSync < DateTime.UtcNow.AddDays(-60))
            {
                _logger.Trace("Issue {0} last updated more than 60 days ago, should refresh.", issue.Title);
                return true;
            }

            if (issue.LastInfoSync >= DateTime.UtcNow.AddHours(-12))
            {
                _logger.Trace("Issue {0} last updated less than 12 hours ago, should not be refreshed.", issue.Title);
                return false;
            }

            if (issue.ReleaseDate > DateTime.UtcNow.AddDays(-30))
            {
                _logger.Trace("Issue {0} released less than 30 days ago, should refresh.", issue.Title);
                return true;
            }

            _logger.Trace("Issue {0} released long ago and recently refreshed, should not be refreshed.", issue.Title);
            return false;
        }
    }
}
