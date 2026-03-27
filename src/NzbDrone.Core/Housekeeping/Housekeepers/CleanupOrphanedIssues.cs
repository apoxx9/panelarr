using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedBooks : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedBooks(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""Books""
                             WHERE ""Id"" IN (
                             SELECT ""Books"".""Id"" FROM ""Books""
                             LEFT OUTER JOIN ""Series""
                             ON ""Books"".""SeriesMetadataId"" = ""Series"".""SeriesMetadataId""
                             WHERE ""Series"".""Id"" IS NULL)");
        }
    }
}
