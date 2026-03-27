using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Books
{
    public interface ISeriesBookLinkRepository : IBasicRepository<SeriesGroupLink>
    {
        List<SeriesGroupLink> GetLinksBySeries(int seriesId);
        List<SeriesGroupLink> GetLinksBySeriesAndSeries(int seriesId, string foreignSeriesId);
        List<SeriesGroupLink> GetLinksByBook(List<int> bookIds);
    }

    public class SeriesGroupLinkRepository : BasicRepository<SeriesGroupLink>, ISeriesBookLinkRepository
    {
        public SeriesGroupLinkRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<SeriesGroupLink> GetLinksBySeries(int seriesId)
        {
            return Query(x => x.SeriesId == seriesId);
        }

        public List<SeriesGroupLink> GetLinksBySeriesAndSeries(int seriesId, string foreignSeriesId)
        {
            return _database.Query<SeriesGroupLink>(
                Builder()
                    .Join<SeriesGroupLink, Issue>((l, b) => l.IssueId == b.Id)
                    .Join<Issue, SeriesMetadata>((b, a) => b.SeriesMetadataId == a.Id)
                    .Where<SeriesGroupLink>(x => x.SeriesId == seriesId)
                    .Where<SeriesMetadata>(a => a.ForeignSeriesId == foreignSeriesId))
                .ToList();
        }

        public List<SeriesGroupLink> GetLinksByBook(List<int> bookIds)
        {
            return _database.QueryJoined<SeriesGroupLink, SeriesGroup>(
                Builder()
                .Join<SeriesGroupLink, SeriesGroup>((l, s) => l.SeriesId == s.Id)
                .Where<SeriesGroupLink>(x => bookIds.Contains(x.IssueId)),
                (link, series) =>
                {
                    link.SeriesGroup = series;
                    return link;
                })
                .ToList();
        }
    }
}
