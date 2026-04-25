using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace NzbDrone.Core.Issues
{
    public interface IMonitorNewIssueService
    {
        bool ShouldMonitorNewIssue(Issue addedIssue, List<Issue> existingIssues, NewItemMonitorTypes series);
    }

    public class MonitorNewIssueService : IMonitorNewIssueService
    {
        private readonly Logger _logger;

        public MonitorNewIssueService(Logger logger)
        {
            _logger = logger;
        }

        public bool ShouldMonitorNewIssue(Issue addedIssue, List<Issue> existingIssues, NewItemMonitorTypes monitorNewItems)
        {
            if (monitorNewItems == NewItemMonitorTypes.None)
            {
                return false;
            }

            if (monitorNewItems == NewItemMonitorTypes.All)
            {
                return true;
            }

            if (monitorNewItems == NewItemMonitorTypes.New)
            {
                var newest = existingIssues.MaxBy(x => x.ReleaseDate ?? DateTime.MinValue)?.ReleaseDate ?? DateTime.MinValue;

                return (addedIssue.ReleaseDate ?? DateTime.MinValue) >= newest;
            }

            throw new NotImplementedException($"Unknown new item monitor type {monitorNewItems}");
        }
    }
}
