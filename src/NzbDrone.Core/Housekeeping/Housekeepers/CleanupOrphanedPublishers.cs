using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedPublishers : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedPublishers(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();

            // Only provider-created publishers (ForeignPublisherId set) are
            // cleaned; publishers created through the API are kept even when
            // nothing references them yet.
            mapper.Execute(@"DELETE FROM ""Publishers""
                            WHERE ""ForeignPublisherId"" IS NOT NULL
                            AND ""Id"" NOT IN (
                            SELECT DISTINCT ""PublisherId"" FROM ""SeriesMetadata""
                            WHERE ""PublisherId"" IS NOT NULL)");
        }
    }
}
