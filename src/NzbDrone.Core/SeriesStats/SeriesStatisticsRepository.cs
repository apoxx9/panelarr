using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.SeriesStats
{
    public interface ISeriesStatisticsRepository
    {
        List<IssueStatistics> SeriesStatistics();
        List<IssueStatistics> SeriesStatistics(int authorId);
    }

    public class SeriesStatisticsRepository : ISeriesStatisticsRepository
    {
        private const string _selectTemplate = "SELECT /**select**/ FROM \"Issues\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/ /**orderby**/";

        private readonly IMainDatabase _database;

        public SeriesStatisticsRepository(IMainDatabase database)
        {
            _database = database;
        }

        public List<IssueStatistics> SeriesStatistics()
        {
            return Query(Builder());
        }

        public List<IssueStatistics> SeriesStatistics(int authorId)
        {
            return Query(Builder().Where<Series>(x => x.Id == authorId));
        }

        private List<IssueStatistics> Query(SqlBuilder builder)
        {
            var sql = builder.AddTemplate(_selectTemplate).LogQuery();

            using (var conn = _database.OpenConnection())
            {
                return conn.Query<IssueStatistics>(sql.RawSql, sql.Parameters).ToList();
            }
        }

        private SqlBuilder Builder()
        {
            var trueIndicator = _database.DatabaseType == DatabaseType.PostgreSQL ? "true" : "1";

            return new SqlBuilder(_database.DatabaseType)
            .Select($@"""Series"".""Id"" AS ""SeriesId"",
                     ""Issues"".""Id"" AS ""IssueId"",
                     SUM(COALESCE(""ComicFiles"".""Size"", 0)) AS ""SizeOnDisk"",
                     1 AS ""TotalBookCount"",
                     CASE WHEN MIN(""ComicFiles"".""Id"") IS NULL THEN 0 ELSE 1 END AS ""AvailableBookCount"",
                     CASE WHEN (""Issues"".""Monitored"" = {trueIndicator} AND (""Issues"".""ReleaseDate"" < @currentDate) OR ""Issues"".""ReleaseDate"" IS NULL) OR MIN(""ComicFiles"".""Id"") IS NOT NULL THEN 1 ELSE 0 END AS ""IssueCount"",
                     CASE WHEN MIN(""ComicFiles"".""Id"") IS NULL THEN 0 ELSE COUNT(""ComicFiles"".""Id"") END AS ""ComicFileCount""")
            .Join<Issue, Series>((issue, author) => issue.SeriesMetadataId == author.SeriesMetadataId)
            .LeftJoin<Issue, ComicFile>((b, f) => b.Id == f.IssueId)
            .Where<Issue>(x => x.Monitored == true)
            .GroupBy<Series>(x => x.Id)
            .GroupBy<Issue>(x => x.Id)
            .AddParameters(new Dictionary<string, object> { { "currentDate", DateTime.UtcNow } });
        }
    }
}
