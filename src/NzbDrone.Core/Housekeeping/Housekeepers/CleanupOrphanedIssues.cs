using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedIssues : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedIssues(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""Issues""
                             WHERE ""Id"" IN (
                             SELECT ""Issues"".""Id"" FROM ""Issues""
                             LEFT OUTER JOIN ""Series""
                             ON ""Issues"".""SeriesMetadataId"" = ""Series"".""SeriesMetadataId""
                             WHERE ""Series"".""Id"" IS NULL)");
        }
    }
}
