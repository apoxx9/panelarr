using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Download.Aggregation.Aggregators;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Aggregation
{
    public interface IRemoteIssueAggregationService
    {
        RemoteIssue Augment(RemoteIssue remoteIssue);
    }

    public class RemoteIssueAggregationService : IRemoteIssueAggregationService
    {
        private readonly IEnumerable<IAggregateRemoteIssue> _augmenters;
        private readonly Logger _logger;

        public RemoteIssueAggregationService(IEnumerable<IAggregateRemoteIssue> augmenters,
                                  Logger logger)
        {
            _augmenters = augmenters;
            _logger = logger;
        }

        public RemoteIssue Augment(RemoteIssue remoteIssue)
        {
            foreach (var augmenter in _augmenters)
            {
                try
                {
                    augmenter.Aggregate(remoteIssue);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, ex.Message);
                }
            }

            return remoteIssue;
        }
    }
}
