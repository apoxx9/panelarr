using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Books
{
    public interface IBookMonitoredService
    {
        void SetBookMonitoredStatus(Series author, MonitoringOptions monitoringOptions);
    }

    public class IssueMonitoredService : IBookMonitoredService
    {
        private readonly ISeriesService _authorService;
        private readonly IBookService _bookService;
        private readonly Logger _logger;

        public IssueMonitoredService(ISeriesService authorService, IBookService bookService, Logger logger)
        {
            _authorService = authorService;
            _bookService = bookService;
            _logger = logger;
        }

        public void SetBookMonitoredStatus(Series author, MonitoringOptions monitoringOptions)
        {
            if (monitoringOptions != null)
            {
                _logger.Debug("[{0}] Setting issue monitored status.", author.Name);

                var issues = _bookService.GetBooksBySeries(author.Id);

                var booksWithFiles = _bookService.GetSeriesBooksWithFiles(author);

                var booksWithoutFiles = issues.Where(c => !booksWithFiles.Select(e => e.Id).Contains(c.Id) && c.ReleaseDate <= DateTime.UtcNow).ToList();

                var monitoredBooks = monitoringOptions.IssuesToMonitor;

                // If specific issues are passed use those instead of the monitoring options.
                if (monitoredBooks.Any())
                {
                    ToggleBooksMonitoredState(
                        issues.Where(s => monitoredBooks.Contains(s.ForeignIssueId)), true);
                    ToggleBooksMonitoredState(
                        issues.Where(s => !monitoredBooks.Contains(s.ForeignIssueId)), false);
                }
                else
                {
                    switch (monitoringOptions.Monitor)
                    {
                        case MonitorTypes.All:
                            ToggleBooksMonitoredState(issues, true);
                            break;
                        case MonitorTypes.Future:
                            _logger.Debug("Unmonitoring Books with Files");
                            ToggleBooksMonitoredState(issues.Where(e => booksWithFiles.Select(c => c.Id).Contains(e.Id)), false);
                            _logger.Debug("Unmonitoring Books without Files");
                            ToggleBooksMonitoredState(issues.Where(e => booksWithoutFiles.Select(c => c.Id).Contains(e.Id)), false);
                            break;
                        case MonitorTypes.None:
                            ToggleBooksMonitoredState(issues, false);
                            break;
                        case MonitorTypes.Missing:
                            _logger.Debug("Unmonitoring Books with Files");
                            ToggleBooksMonitoredState(issues.Where(e => booksWithFiles.Select(c => c.Id).Contains(e.Id)), false);
                            _logger.Debug("Monitoring Books without Files");
                            ToggleBooksMonitoredState(issues.Where(e => booksWithoutFiles.Select(c => c.Id).Contains(e.Id)), true);
                            break;
                        case MonitorTypes.Existing:
                            _logger.Debug("Monitoring Books with Files");
                            ToggleBooksMonitoredState(issues.Where(e => booksWithFiles.Select(c => c.Id).Contains(e.Id)), true);
                            _logger.Debug("Unmonitoring Books without Files");
                            ToggleBooksMonitoredState(issues.Where(e => booksWithoutFiles.Select(c => c.Id).Contains(e.Id)), false);
                            break;
                        case MonitorTypes.Latest:
                            ToggleBooksMonitoredState(issues, false);
                            ToggleBooksMonitoredState(issues.OrderByDescending(e => e.ReleaseDate).Take(1), true);
                            break;
                        case MonitorTypes.First:
                            ToggleBooksMonitoredState(issues, false);
                            ToggleBooksMonitoredState(issues.OrderBy(e => e.ReleaseDate).Take(1), true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                // Use individual update to ensure updates are sent to frontend
                foreach (var issue in issues)
                {
                    _bookService.UpdateBook(issue);
                }
            }

            _authorService.UpdateSeries(author);
        }

        private void ToggleBooksMonitoredState(IEnumerable<Issue> issues, bool monitored)
        {
            foreach (var issue in issues)
            {
                issue.Monitored = monitored;
            }
        }
    }
}
