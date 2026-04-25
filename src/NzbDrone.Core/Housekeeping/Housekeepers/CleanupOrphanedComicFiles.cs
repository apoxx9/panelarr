using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedComicFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedComicFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();

            // Unlink where issues no longer exists
            mapper.Execute(@"UPDATE ""ComicFiles""
                             SET ""IssueId"" = 0
                             WHERE ""Id"" IN (
                             SELECT ""ComicFiles"".""Id"" FROM ""ComicFiles""
                             LEFT OUTER JOIN ""Issues""
                             ON ""ComicFiles"".""IssueId"" = ""Issues"".""Id""
                             WHERE ""Issues"".""Id"" IS NULL)");
        }
    }
}
