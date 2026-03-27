using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedBookFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedBookFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();

            // Unlink where issues no longer exists
            mapper.Execute(@"UPDATE ""ComicFiles""
                             SET ""EditionId"" = 0
                             WHERE ""Id"" IN (
                             SELECT ""ComicFiles"".""Id"" FROM ""ComicFiles""
                             LEFT OUTER JOIN ""Editions""
                             ON ""ComicFiles"".""EditionId"" = ""Editions"".""Id""
                             WHERE ""Editions"".""Id"" IS NULL)");
        }
    }
}
