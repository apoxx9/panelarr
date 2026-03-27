using System.Collections.Generic;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Blocklisting
{
    public interface IBlocklistRepository : IBasicRepository<Blocklist>
    {
        List<Blocklist> BlocklistedByTitle(int authorId, string sourceTitle);
        List<Blocklist> BlocklistedByTorrentInfoHash(int authorId, string torrentInfoHash);
        List<Blocklist> BlocklistedBySeries(int authorId);
    }

    public class BlocklistRepository : BasicRepository<Blocklist>, IBlocklistRepository
    {
        public BlocklistRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Blocklist> BlocklistedByTitle(int authorId, string sourceTitle)
        {
            return Query(e => e.SeriesId == authorId && e.SourceTitle.Contains(sourceTitle));
        }

        public List<Blocklist> BlocklistedByTorrentInfoHash(int authorId, string torrentInfoHash)
        {
            return Query(e => e.SeriesId == authorId && e.TorrentInfoHash.Contains(torrentInfoHash));
        }

        public List<Blocklist> BlocklistedBySeries(int authorId)
        {
            return Query(b => b.SeriesId == authorId);
        }

        protected override SqlBuilder PagedBuilder() => new SqlBuilder(_database.DatabaseType)
            .Join<Blocklist, Series>((b, m) => b.SeriesId == m.Id)
            .Join<Series, SeriesMetadata>((l, r) => l.SeriesMetadataId == r.Id);
        protected override IEnumerable<Blocklist> PagedQuery(SqlBuilder builder) => _database.QueryJoined<Blocklist, Series, SeriesMetadata>(builder,
            (bl, author, metadata) =>
                    {
                        author.Metadata = metadata;
                        bl.Series = author;
                        return bl;
                    });
    }
}
