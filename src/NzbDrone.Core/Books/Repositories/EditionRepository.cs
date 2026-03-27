using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Books
{
    public interface IEditionRepository : IBasicRepository<Edition>
    {
        List<Edition> GetAllMonitoredEditions();
        Edition FindByForeignEditionId(string foreignEditionId);
        List<Edition> FindByBook(IEnumerable<int> ids);
        List<Edition> FindBySeries(int id);
        List<Edition> FindBySeriesMetadataId(int id, bool onlyMonitored);
        Edition FindByTitle(int authorMetadataId, string title);
        List<Edition> GetEditionsForRefresh(int bookId, List<string> foreignEditionIds);
        List<Edition> SetMonitored(Edition edition);
    }

    public class EditionRepository : BasicRepository<Edition>, IEditionRepository
    {
        public EditionRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Edition> GetAllMonitoredEditions()
        {
            return Query(x => x.Monitored == true);
        }

        public Edition FindByForeignEditionId(string foreignEditionId)
        {
            var edition = Query(x => x.ForeignEditionId == foreignEditionId).SingleOrDefault();

            return edition;
        }

        public List<Edition> GetEditionsForRefresh(int bookId, List<string> foreignEditionIds)
        {
            return Query(r => r.IssueId == bookId || foreignEditionIds.Contains(r.ForeignEditionId));
        }

        public List<Edition> FindByBook(IEnumerable<int> ids)
        {
            // populate the issues and author metadata also
            // this hopefully speeds up the track matching a lot
            var builder = new SqlBuilder(_database.DatabaseType)
                .LeftJoin<Edition, Issue>((e, b) => e.IssueId == b.Id)
                .LeftJoin<Issue, SeriesMetadata>((b, a) => b.SeriesMetadataId == a.Id)
                .Where<Edition>(r => ids.Contains(r.IssueId));

            return _database.QueryJoined<Edition, Issue, SeriesMetadata>(builder, (edition, issue, metadata) =>
                    {
                        if (issue != null)
                        {
                            issue.SeriesMetadata = metadata;
                            edition.Issue = issue;
                        }

                        return edition;
                    }).ToList();
        }

        public List<Edition> FindBySeries(int id)
        {
            return Query(Builder().Join<Edition, Issue>((e, b) => e.IssueId == b.Id)
                         .Join<Issue, Series>((b, a) => b.SeriesMetadataId == a.SeriesMetadataId)
                         .Where<Series>(a => a.Id == id));
        }

        public List<Edition> FindBySeriesMetadataId(int authorMetadataId, bool onlyMonitored)
        {
            var builder = Builder().Join<Edition, Issue>((e, b) => e.IssueId == b.Id)
                .Where<Issue>(b => b.SeriesMetadataId == authorMetadataId);

            if (onlyMonitored)
            {
                builder = builder.OrWhere<Edition>(e => e.Monitored == true);
            }

            return Query(builder);
        }

        public Edition FindByTitle(int authorMetadataId, string title)
        {
            return Query(Builder().Join<Edition, Issue>((e, b) => e.IssueId == b.Id)
                .Where<Issue>(b => b.SeriesMetadataId == authorMetadataId)
                .Where<Edition>(e => e.Monitored == true)
                .Where<Edition>(e => e.Title == title))
                .FirstOrDefault();
        }

        public List<Edition> SetMonitored(Edition edition)
        {
            var allEditions = FindByBook(new[] { edition.IssueId });
            allEditions.ForEach(r => r.Monitored = r.Id == edition.Id);
            Ensure.That(allEditions.Count(x => x.Monitored) == 1).IsTrue();
            UpdateMany(allEditions);
            return allEditions;
        }
    }
}
