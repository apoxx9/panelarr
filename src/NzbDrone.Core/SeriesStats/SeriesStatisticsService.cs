using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.SeriesStats
{
    public interface ISeriesStatisticsService
    {
        List<SeriesStatistics> SeriesStatistics();
        SeriesStatistics SeriesStatistics(int seriesId);
    }

    public class SeriesStatisticsService : ISeriesStatisticsService,
        IHandle<SeriesAddedEvent>,
        IHandle<SeriesUpdatedEvent>,
        IHandle<SeriesDeletedEvent>,
        IHandle<IssueAddedEvent>,
        IHandle<IssueDeletedEvent>,
        IHandle<IssueImportedEvent>,
        IHandle<IssueEditedEvent>,
        IHandle<IssueUpdatedEvent>,
        IHandle<ComicFileDeletedEvent>
    {
        private readonly ISeriesStatisticsRepository _seriesStatisticsRepository;
        private readonly ICached<List<IssueStatistics>> _cache;

        public SeriesStatisticsService(ISeriesStatisticsRepository seriesStatisticsRepository,
                                       ICacheManager cacheManager)
        {
            _seriesStatisticsRepository = seriesStatisticsRepository;
            _cache = cacheManager.GetCache<List<IssueStatistics>>(GetType());
        }

        public List<SeriesStatistics> SeriesStatistics()
        {
            var issueStatistics = _cache.Get("AllSeries", () => _seriesStatisticsRepository.SeriesStatistics());

            return issueStatistics.GroupBy(s => s.SeriesId).Select(s => MapSeriesStatistics(s.ToList())).ToList();
        }

        public SeriesStatistics SeriesStatistics(int seriesId)
        {
            var stats = _cache.Get(seriesId.ToString(), () => _seriesStatisticsRepository.SeriesStatistics(seriesId));

            if (stats == null || stats.Count == 0)
            {
                return new SeriesStatistics();
            }

            return MapSeriesStatistics(stats);
        }

        private SeriesStatistics MapSeriesStatistics(List<IssueStatistics> issueStatistics)
        {
            var seriesStatistics = new SeriesStatistics
            {
                SeriesId = issueStatistics.First().SeriesId,
                ComicFileCount = issueStatistics.Sum(s => s.ComicFileCount),
                IssueCount = issueStatistics.Sum(s => s.IssueCount),
                AvailableIssueCount = issueStatistics.Sum(s => s.AvailableIssueCount),
                TotalIssueCount = issueStatistics.Sum(s => s.TotalIssueCount),
                SizeOnDisk = issueStatistics.Sum(s => s.SizeOnDisk),
                IssueStatistics = issueStatistics
            };

            return seriesStatistics;
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(SeriesAddedEvent message)
        {
            _cache.Remove("AllSeries");
            _cache.Remove(message.Series.Id.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(SeriesUpdatedEvent message)
        {
            _cache.Remove("AllSeries");
            _cache.Remove(message.Series.Id.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(SeriesDeletedEvent message)
        {
            _cache.Remove("AllSeries");
            _cache.Remove(message.Series.Id.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(IssueAddedEvent message)
        {
            _cache.Remove("AllSeries");
            _cache.Remove(message.Issue.SeriesId.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(IssueDeletedEvent message)
        {
            _cache.Remove("AllSeries");
            _cache.Remove(message.Issue.SeriesId.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(IssueImportedEvent message)
        {
            _cache.Remove("AllSeries");
            _cache.Remove(message.Series.Id.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(IssueEditedEvent message)
        {
            _cache.Remove("AllSeries");
            _cache.Remove(message.Issue.SeriesId.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(IssueUpdatedEvent message)
        {
            _cache.Remove("AllSeries");
            _cache.Remove(message.Issue.SeriesId.ToString());
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(ComicFileDeletedEvent message)
        {
            _cache.Remove("AllSeries");

            var seriesId = message.ComicFile.Series?.Value?.Id.ToString();
            if (seriesId != null)
            {
                _cache.Remove(seriesId);
            }
        }
    }
}
