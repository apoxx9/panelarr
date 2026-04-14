using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.History
{
    public interface IHistoryRepository : IBasicRepository<EntityHistory>
    {
        EntityHistory MostRecentForIssue(int issueId);
        EntityHistory MostRecentForDownloadId(string downloadId);
        List<EntityHistory> FindByDownloadId(string downloadId);
        List<EntityHistory> GetBySeries(int seriesId, EntityHistoryEventType? eventType);
        List<EntityHistory> GetByIssue(int issueId, EntityHistoryEventType? eventType);
        List<EntityHistory> FindDownloadHistory(int idSeriesId, QualityModel quality);
        void DeleteForSeries(int seriesId);
        List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType);
    }

    public class HistoryRepository : BasicRepository<EntityHistory>, IHistoryRepository
    {
        public HistoryRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public EntityHistory MostRecentForIssue(int issueId)
        {
            return Query(h => h.IssueId == issueId).MaxBy(h => h.Date);
        }

        public EntityHistory MostRecentForDownloadId(string downloadId)
        {
            return Query(h => h.DownloadId == downloadId).MaxBy(h => h.Date);
        }

        public List<EntityHistory> FindByDownloadId(string downloadId)
        {
            return _database.QueryJoined<EntityHistory, Series, Issue>(
                Builder()
                .Join<EntityHistory, Series>((h, a) => h.SeriesId == a.Id)
                .Join<EntityHistory, Issue>((h, a) => h.IssueId == a.Id)
                .Where<EntityHistory>(h => h.DownloadId == downloadId),
                (history, series, issue) =>
                {
                    history.Series = series;
                    history.Issue = issue;
                    return history;
                }).ToList();
        }

        public List<EntityHistory> GetBySeries(int seriesId, EntityHistoryEventType? eventType)
        {
            var builder = Builder().Where<EntityHistory>(h => h.SeriesId == seriesId);

            if (eventType.HasValue)
            {
                builder.Where<EntityHistory>(h => h.EventType == eventType);
            }

            return Query(builder).OrderByDescending(h => h.Date).ToList();
        }

        public List<EntityHistory> GetByIssue(int issueId, EntityHistoryEventType? eventType)
        {
            var builder = Builder()
                .Join<EntityHistory, Issue>((h, a) => h.IssueId == a.Id)
                .Where<EntityHistory>(h => h.IssueId == issueId);

            if (eventType.HasValue)
            {
                builder.Where<EntityHistory>(h => h.EventType == eventType);
            }

            return _database.QueryJoined<EntityHistory, Issue>(
                builder,
                (history, issue) =>
                {
                    history.Issue = issue;
                    return history;
                }).OrderByDescending(h => h.Date).ToList();
        }

        public List<EntityHistory> FindDownloadHistory(int idSeriesId, QualityModel quality)
        {
            var allowed = new[] { (int)EntityHistoryEventType.Grabbed, (int)EntityHistoryEventType.DownloadFailed, (int)EntityHistoryEventType.ComicFileImported };

            return Query(h => h.SeriesId == idSeriesId &&
                         h.Quality == quality &&
                         allowed.Contains((int)h.EventType));
        }

        public void DeleteForSeries(int seriesId)
        {
            Delete(c => c.SeriesId == seriesId);
        }

        protected override SqlBuilder PagedBuilder() => new SqlBuilder(_database.DatabaseType)
            .Join<EntityHistory, Series>((h, a) => h.SeriesId == a.Id)
            .Join<Series, SeriesMetadata>((l, r) => l.SeriesMetadataId == r.Id)
            .Join<EntityHistory, Issue>((h, a) => h.IssueId == a.Id);

        protected override IEnumerable<EntityHistory> PagedQuery(SqlBuilder builder) =>
            _database.QueryJoined<EntityHistory, Series, SeriesMetadata, Issue>(builder, (history, series, metadata, issue) =>
                    {
                        series.Metadata = metadata;
                        history.Series = series;
                        history.Issue = issue;
                        return history;
                    });

        public List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType)
        {
            var builder = Builder()
                .Join<EntityHistory, Series>((h, a) => h.SeriesId == a.Id)
                .LeftJoin<EntityHistory, Issue>((h, b) => h.IssueId == b.Id)
                .Where<EntityHistory>(x => x.Date >= date);

            if (eventType.HasValue)
            {
                builder.Where<EntityHistory>(h => h.EventType == eventType);
            }

            return _database.QueryJoined<EntityHistory, Series, Issue>(builder, (history, series, issue) =>
            {
                history.Series = series;
                history.Issue = issue;
                return history;
            }).OrderBy(h => h.Date).ToList();
        }
    }
}
