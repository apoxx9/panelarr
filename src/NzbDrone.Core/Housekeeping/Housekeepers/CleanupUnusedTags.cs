using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupUnusedTags : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupUnusedTags(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            var usedTags = new[]
                {
                    ("Series", "Tags"),
                    ("Notifications", "Tags"),
                    ("DelayProfiles", "Tags"),
                    ("ReleaseProfiles", "Tags"),
                    ("ImportLists", "Tags"),
                    ("Indexers", "Tags"),
                    ("DownloadClients", "Tags"),
                    ("RootFolders", "DefaultTags")
                }
                .SelectMany(v => GetUsedTags(v.Item1, v.Item2, mapper))
                .Distinct()
                .ToArray();

            if (usedTags.Any())
            {
                var usedTagsList = usedTags.Select(d => d.ToString()).Join(",");

                if (_database.DatabaseType == DatabaseType.PostgreSQL)
                {
                    mapper.Execute($"DELETE FROM \"Tags\" WHERE NOT \"Id\" = ANY (\'{{{usedTagsList}}}\'::int[])");
                }
                else
                {
                    mapper.Execute($"DELETE FROM \"Tags\" WHERE NOT \"Id\" IN ({usedTagsList})");
                }
            }
            else
            {
                mapper.Execute("DELETE FROM \"Tags\"");
            }
        }

        private int[] GetUsedTags(string table, string column, IDbConnection mapper)
        {
            return mapper.Query<List<int>>($"SELECT DISTINCT \"{column}\" FROM \"{table}\" WHERE NOT \"{column}\" = '[]' AND NOT \"{column}\" IS NULL")
                .SelectMany(x => x)
                .Distinct()
                .ToArray();
        }
    }
}
