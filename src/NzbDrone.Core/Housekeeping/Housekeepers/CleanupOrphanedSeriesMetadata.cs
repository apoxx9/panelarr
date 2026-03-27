using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedSeriesMetadata : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedSeriesMetadata(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""SeriesMetadata""
                             WHERE ""Id"" IN (
                             SELECT ""SeriesMetadata"".""Id"" FROM ""SeriesMetadata""
                             LEFT OUTER JOIN ""Books"" ON ""Books"".""SeriesMetadataId"" = ""SeriesMetadata"".""Id""
                             LEFT OUTER JOIN ""Series"" ON ""Series"".""SeriesMetadataId"" = ""SeriesMetadata"".""Id""
                             WHERE ""Books"".""Id"" IS NULL AND ""Series"".""Id"" IS NULL)");
        }
    }
}
