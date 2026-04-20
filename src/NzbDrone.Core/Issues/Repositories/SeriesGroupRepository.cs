using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface ISeriesGroupRepository : IBasicRepository<SeriesGroup>
    {
        SeriesGroup FindById(string foreignSeriesId);
        List<SeriesGroup> FindById(List<string> foreignSeriesId);
        List<SeriesGroup> GetBySeriesMetadataId(int seriesMetadataId);
        List<SeriesGroup> GetBySeriesId(int seriesId);
    }

    public class SeriesGroupRepository : BasicRepository<SeriesGroup>, ISeriesGroupRepository
    {
        public SeriesGroupRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public SeriesGroup FindById(string foreignSeriesId)
        {
            return Query(x => x.ForeignSeriesGroupId == foreignSeriesId).SingleOrDefault();
        }

        public List<SeriesGroup> FindById(List<string> foreignSeriesId)
        {
            return Query(x => foreignSeriesId.Contains(x.ForeignSeriesGroupId));
        }

        public List<SeriesGroup> GetBySeriesMetadataId(int seriesMetadataId)
        {
            return QueryDistinct(Builder().Join<SeriesGroup, SeriesGroupLink>((l, r) => l.Id == r.SeriesGroupId)
                                 .Where<SeriesGroupLink>(x => x.SeriesMetadataId == seriesMetadataId));
        }

        public List<SeriesGroup> GetBySeriesId(int seriesId)
        {
            return QueryDistinct(Builder().Join<SeriesGroup, SeriesGroupLink>((l, r) => l.Id == r.SeriesGroupId)
                                 .Join<SeriesGroupLink, Series>((l, r) => l.SeriesMetadataId == r.SeriesMetadataId)
                                 .Where<Series>(x => x.Id == seriesId));
        }
    }
}
