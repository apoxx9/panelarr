using System.Collections.Generic;
using System.Data;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    // Download-client settings properties carried Lidarr/Sonarr lineage names
    // (MusicCategory, TvDirectory, OlderTvPriority...). The C# properties are
    // renamed to Comic*/Issue*; stored provider-settings JSON must follow or
    // every configured download client silently loses those values.
    [Migration(13)]
    public class RenameDownloadClientSettingKeys : NzbDroneMigrationBase
    {
        private static readonly Dictionary<string, string> KeyMap = new Dictionary<string, string>
        {
            { "musicCategory", "comicCategory" },
            { "musicImportedCategory", "comicImportedCategory" },
            { "musicDirectory", "comicDirectory" },
            { "tvDirectory", "comicDirectory" },
            { "olderTvPriority", "olderIssuePriority" },
            { "recentTvPriority", "recentIssuePriority" }
        };

        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(MigrateSettings);
        }

        private void MigrateSettings(IDbConnection conn, IDbTransaction tran)
        {
            var updates = new List<(int Id, string Settings)>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"Id\", \"Settings\" FROM \"DownloadClients\"";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        var settings = reader.IsDBNull(1) ? null : reader.GetString(1);

                        if (string.IsNullOrWhiteSpace(settings))
                        {
                            continue;
                        }

                        var json = JObject.Parse(settings);
                        var changed = false;

                        foreach (var pair in KeyMap)
                        {
                            var property = json.Property(pair.Key, System.StringComparison.OrdinalIgnoreCase);

                            if (property != null)
                            {
                                json[pair.Value] = property.Value;
                                property.Remove();
                                changed = true;
                            }
                        }

                        if (changed)
                        {
                            updates.Add((id, json.ToString()));
                        }
                    }
                }
            }

            foreach (var (id, settings) in updates)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = "UPDATE \"DownloadClients\" SET \"Settings\" = ? WHERE \"Id\" = ?";

                    var settingsParam = cmd.CreateParameter();
                    settingsParam.Value = settings;
                    cmd.Parameters.Add(settingsParam);

                    var idParam = cmd.CreateParameter();
                    idParam.Value = id;
                    cmd.Parameters.Add(idParam);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
