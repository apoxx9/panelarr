using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Books
{
    public interface ISeriesRepository : IBasicRepository<Series>
    {
        bool SeriesPathExists(string path);
        Series FindByName(string cleanName);
        Series FindById(string foreignSeriesId);
        Dictionary<int, string> AllSeriesPaths();
        Dictionary<int, List<int>> AllSeriesTags();
        Series GetSeriesByMetadataId(int authorMetadataId);
        List<Series> GetSeriessByMetadataId(IEnumerable<int> authorMetadataId);
    }

    public class SeriesRepository : BasicRepository<Series>, ISeriesRepository
    {
        public SeriesRepository(IMainDatabase database,
                                IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        protected override SqlBuilder Builder() => new SqlBuilder(_database.DatabaseType)
            .Join<Series, SeriesMetadata>((a, m) => a.SeriesMetadataId == m.Id);

        protected override List<Series> Query(SqlBuilder builder) => Query(_database, builder).ToList();

        public static IEnumerable<Series> Query(IDatabase database, SqlBuilder builder)
        {
            return database.QueryJoined<Series, SeriesMetadata>(builder, (author, metadata) =>
                    {
                        author.Metadata = metadata;
                        return author;
                    });
        }

        public bool SeriesPathExists(string path)
        {
            return Query(c => c.Path == path).Any();
        }

        public Series FindById(string foreignSeriesId)
        {
            return Query(Builder().Where<SeriesMetadata>(m => m.ForeignSeriesId == foreignSeriesId)).SingleOrDefault();
        }

        public Series FindByName(string cleanName)
        {
            cleanName = cleanName.ToLowerInvariant();

            return Query(s => s.CleanName == cleanName).ExclusiveOrDefault();
        }

        public Dictionary<int, string> AllSeriesPaths()
        {
            using (var conn = _database.OpenConnection())
            {
                var strSql = "SELECT \"Id\" AS \"Key\", \"Path\" AS \"Value\" FROM \"Seriess\"";
                return conn.Query<KeyValuePair<int, string>>(strSql).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public Dictionary<int, List<int>> AllSeriesTags()
        {
            using (var conn = _database.OpenConnection())
            {
                var strSql = "SELECT \"Id\" AS \"Key\", \"Tags\" AS \"Value\" FROM \"Seriess\" WHERE \"Tags\" IS NOT NULL";
                return conn.Query<KeyValuePair<int, List<int>>>(strSql).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public Series GetSeriesByMetadataId(int authorMetadataId)
        {
            return Query(s => s.SeriesMetadataId == authorMetadataId).SingleOrDefault();
        }

        public List<Series> GetSeriessByMetadataId(IEnumerable<int> authorMetadataIds)
        {
            return Query(s => authorMetadataIds.Contains(s.SeriesMetadataId));
        }
    }
}
