using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedSeriesIssueLinks : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedSeriesIssueLinks(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""SeriesGroupLink""
                            WHERE ""Id"" IN (
                            SELECT ""SeriesGroupLink"".""Id"" FROM ""SeriesGroupLink""
                            LEFT OUTER JOIN ""Issues""
                            ON ""SeriesGroupLink"".""IssueId"" = ""Issues"".""Id""
                            WHERE ""Issues"".""Id"" IS NULL)");

            mapper.Execute(@"DELETE FROM ""SeriesGroupLink""
                             WHERE ""Id"" IN (
                             SELECT ""SeriesGroupLink"".""Id"" FROM ""SeriesGroupLink""
                             LEFT OUTER JOIN ""SeriesGroup""
                             ON ""SeriesGroupLink"".""SeriesId"" = ""SeriesGroup"".""Id""
                             WHERE ""SeriesGroup"".""Id"" IS NULL)");
        }
    }
}
