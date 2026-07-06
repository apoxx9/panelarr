using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues.Relations
{
    public interface ISeriesRelationRepository : IBasicRepository<SeriesRelation>
    {
        List<SeriesRelation> FindBySeriesId(int seriesId);
    }

    public class SeriesRelationRepository : BasicRepository<SeriesRelation>, ISeriesRelationRepository
    {
        public SeriesRelationRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<SeriesRelation> FindBySeriesId(int seriesId)
        {
            // Links render symmetrically, so a series' relations include both
            // the ones it points at and the ones pointing at it.
            return Query(r => r.SeriesId == seriesId || r.RelatedSeriesId == seriesId);
        }
    }
}
