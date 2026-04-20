using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface ISeriesIssueLinkRepository : IBasicRepository<SeriesGroupLink>
    {
        List<SeriesGroupLink> GetLinksBySeries(int seriesId);
        List<SeriesGroupLink> GetLinksBySeriesAndSeries(int seriesId, string foreignSeriesId);
        List<SeriesGroupLink> GetLinksByIssue(List<int> issueIds);
    }

    public class SeriesGroupLinkRepository : BasicRepository<SeriesGroupLink>, ISeriesIssueLinkRepository
    {
        public SeriesGroupLinkRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<SeriesGroupLink> GetLinksBySeries(int seriesGroupId)
        {
            return Query(x => x.SeriesGroupId == seriesGroupId);
        }

        public List<SeriesGroupLink> GetLinksBySeriesAndSeries(int seriesGroupId, string foreignSeriesId)
        {
            return _database.Query<SeriesGroupLink>(
                Builder()
                    .Join<SeriesGroupLink, SeriesMetadata>((l, a) => l.SeriesMetadataId == a.Id)
                    .Where<SeriesGroupLink>(x => x.SeriesGroupId == seriesGroupId)
                    .Where<SeriesMetadata>(a => a.ForeignSeriesId == foreignSeriesId))
                .ToList();
        }

        public List<SeriesGroupLink> GetLinksByIssue(List<int> seriesMetadataIds)
        {
            return _database.QueryJoined<SeriesGroupLink, SeriesGroup>(
                Builder()
                .Join<SeriesGroupLink, SeriesGroup>((l, s) => l.SeriesGroupId == s.Id)
                .Where<SeriesGroupLink>(x => seriesMetadataIds.Contains(x.SeriesMetadataId)),
                (link, series) =>
                {
                    link.SeriesGroup = series;
                    return link;
                })
                .ToList();
        }
    }
}
