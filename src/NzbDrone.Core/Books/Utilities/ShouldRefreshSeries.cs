using System;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Books
{
    public interface ICheckIfSeriesShouldBeRefreshed
    {
        bool ShouldRefresh(Series author);
    }

    public class ShouldRefreshSeries : ICheckIfSeriesShouldBeRefreshed
    {
        private readonly IBookService _bookService;
        private readonly Logger _logger;

        public ShouldRefreshSeries(IBookService bookService, Logger logger)
        {
            _bookService = bookService;
            _logger = logger;
        }

        public bool ShouldRefresh(Series author)
        {
            if (author.LastInfoSync < DateTime.UtcNow.AddDays(-30))
            {
                _logger.Trace("Series {0} last updated more than 30 days ago, should refresh.", author.Name);
                return true;
            }

            if (author.LastInfoSync >= DateTime.UtcNow.AddHours(-12))
            {
                _logger.Trace("Series {0} last updated less than 12 hours ago, should not be refreshed.", author.Name);
                return false;
            }

            if (author.Metadata.Value.Status == SeriesStatusType.Continuing && author.LastInfoSync < DateTime.UtcNow.AddDays(-2))
            {
                _logger.Trace("Series {0} is continuing and has not been refreshed in 2 days, should refresh.", author.Name);
                return true;
            }

            var lastBook = _bookService.GetBooksBySeries(author.Id).MaxBy(e => e.ReleaseDate);

            if (lastBook != null && lastBook.ReleaseDate > DateTime.UtcNow.AddDays(-30))
            {
                _logger.Trace("Last issue in {0} released less than 30 days ago, should refresh.", author.Name);
                return true;
            }

            _logger.Trace("Series {0} ended long ago, should not be refreshed.", author.Name);
            return false;
        }
    }
}
