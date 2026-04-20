using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedMetadataFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedMetadataFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            DeleteOrphanedBySeries();
            DeleteOrphanedByIssue();
            DeleteOrphanedByComicFile();
            DeleteWhereIssueIdIsZero();
            DeleteWhereComicFileIsZero();
        }

        private void DeleteOrphanedBySeries()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""MetadataFiles"".""Id"" FROM ""MetadataFiles""
                             LEFT OUTER JOIN ""Series""
                             ON ""MetadataFiles"".""SeriesId"" = ""Series"".""Id""
                             WHERE ""Series"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByIssue()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""MetadataFiles"".""Id"" FROM ""MetadataFiles""
                             LEFT OUTER JOIN ""Issues""
                             ON ""MetadataFiles"".""IssueId"" = ""Issues"".""Id""
                             WHERE ""MetadataFiles"".""IssueId"" > 0
                             AND ""Issues"".""Id"" IS NULL)");
        }

        private void DeleteOrphanedByComicFile()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""MetadataFiles"".""Id"" FROM ""MetadataFiles""
                             LEFT OUTER JOIN ""ComicFiles""
                             ON ""MetadataFiles"".""ComicFileId"" = ""ComicFiles"".""Id""
                             WHERE ""MetadataFiles"".""ComicFileId"" > 0
                             AND ""ComicFiles"".""Id"" IS NULL)");
        }

        private void DeleteWhereIssueIdIsZero()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""Id"" FROM ""MetadataFiles""
                             WHERE ""Type"" IN (2, 4)
                             AND ""IssueId"" = 0)");
        }

        private void DeleteWhereComicFileIsZero()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                             SELECT ""Id"" FROM ""MetadataFiles""
                             WHERE ""Type"" IN (2, 4)
                             AND ""ComicFileId"" = 0)");
        }
    }
}
