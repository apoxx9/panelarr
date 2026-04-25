using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupDuplicateMetadataFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupDuplicateMetadataFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            DeleteDuplicateSeriesMetadata();
            DeleteDuplicateIssueMetadata();
            DeleteDuplicateComicFileMetadata();
        }

        private void DeleteDuplicateSeriesMetadata()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                                 SELECT MIN(""Id"") FROM ""MetadataFiles""
                                 WHERE ""Type"" = 1
                                 GROUP BY ""SeriesId"", ""Consumer""
                                 HAVING COUNT(""SeriesId"") > 1
                             )");
        }

        private void DeleteDuplicateIssueMetadata()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                                 SELECT MIN(""Id"") FROM ""MetadataFiles""
                                 WHERE ""Type"" IN (2, 4)
                                 GROUP BY ""IssueId"", ""Consumer""
                                 HAVING COUNT(""IssueId"") > 1
                             )");
        }

        private void DeleteDuplicateComicFileMetadata()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""MetadataFiles""
                             WHERE ""Id"" IN (
                                 SELECT MIN(""Id"") FROM ""MetadataFiles""
                                 WHERE ""Type"" IN (2, 4)
                                 GROUP BY ""ComicFileId"", ""Consumer""
                                 HAVING COUNT(""ComicFileId"") > 1
                             )");
        }
    }
}
