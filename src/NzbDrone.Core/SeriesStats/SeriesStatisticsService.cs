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
        SeriesStatistics SeriesStatistics(int authorId);
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
        private readonly ISeriesStatisticsRepository _authorStatisticsRepository;
        private readonly ICached<List<IssueStatistics>> _cache;

        public SeriesStatisticsService(ISeriesStatisticsRepository authorStatisticsRepository,
                                       ICacheManager cacheManager)
        {
            _authorStatisticsRepository = authorStatisticsRepository;
            _cache = cacheManager.GetCache<List<IssueStatistics>>(GetType());
        }

        public List<SeriesStatistics> SeriesStatistics()
        {
            var bookStatistics = _cache.Get("AllSeries", () => _authorStatisticsRepository.SeriesStatistics());

            return bookStatistics.GroupBy(s => s.SeriesId).Select(s => MapSeriesStatistics(s.ToList())).ToList();
        }

        public SeriesStatistics SeriesStatistics(int authorId)
        {
            var stats = _cache.Get(authorId.ToString(), () => _authorStatisticsRepository.SeriesStatistics(authorId));

            if (stats == null || stats.Count == 0)
            {
                return new SeriesStatistics();
            }

            return MapSeriesStatistics(stats);
        }

        private SeriesStatistics MapSeriesStatistics(List<IssueStatistics> bookStatistics)
        {
            var authorStatistics = new SeriesStatistics
            {
                SeriesId = bookStatistics.First().SeriesId,
                ComicFileCount = bookStatistics.Sum(s => s.ComicFileCount),
                IssueCount = bookStatistics.Sum(s => s.IssueCount),
                AvailableIssueCount = bookStatistics.Sum(s => s.AvailableIssueCount),
                TotalIssueCount = bookStatistics.Sum(s => s.TotalIssueCount),
                SizeOnDisk = bookStatistics.Sum(s => s.SizeOnDisk),
                IssueStatistics = bookStatistics
            };

            return authorStatistics;
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

            var authorId = message.ComicFile.Series?.Value?.Id.ToString();
            if (authorId != null)
            {
                _cache.Remove(authorId);
            }
        }
    }
}
