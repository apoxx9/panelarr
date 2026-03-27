using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Books
{
    public interface ISeriesGroupRepository : IBasicRepository<SeriesGroup>
    {
        SeriesGroup FindById(string foreignSeriesId);
        List<SeriesGroup> FindById(List<string> foreignSeriesId);
        List<SeriesGroup> GetBySeriesMetadataId(int authorMetadataId);
        List<SeriesGroup> GetBySeriesId(int authorId);
    }

    public class SeriesGroupRepository : BasicRepository<SeriesGroup>, ISeriesGroupRepository
    {
        public SeriesGroupRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public SeriesGroup FindById(string foreignSeriesId)
        {
            return Query(x => x.ForeignSeriesId == foreignSeriesId).SingleOrDefault();
        }

        public List<SeriesGroup> FindById(List<string> foreignSeriesId)
        {
            return Query(x => foreignSeriesId.Contains(x.ForeignSeriesId));
        }

        public List<SeriesGroup> GetBySeriesMetadataId(int authorMetadataId)
        {
            return QueryDistinct(Builder().Join<SeriesGroup, SeriesGroupLink>((l, r) => l.Id == r.SeriesId)
                                 .Join<SeriesGroupLink, Issue>((l, r) => l.IssueId == r.Id)
                                 .Where<Issue>(x => x.SeriesMetadataId == authorMetadataId));
        }

        public List<SeriesGroup> GetBySeriesId(int authorId)
        {
            return QueryDistinct(Builder().Join<SeriesGroup, SeriesGroupLink>((l, r) => l.Id == r.SeriesId)
                                 .Join<SeriesGroupLink, Issue>((l, r) => l.IssueId == r.Id)
                                 .Join<Issue, Series>((l, r) => l.SeriesMetadataId == r.SeriesMetadataId)
                                 .Where<Series>(x => x.Id == authorId));
        }
    }
}
