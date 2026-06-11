using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    // Quality means source fidelity now, not container format. The container
    // qualities CBR(3)/CBZ(4)/CB7(5)/CBZ Web(6)/CBZ HD(7) are retired in
    // favour of Scan(8)/C2C(9)/Archive(10)/WebRip(11)/Digital(12).
    //
    // - Quality profiles: any allowed container rung enables the whole
    //   source range; container cutoffs become Digital.
    // - ComicFiles: the stored quality is re-derived from the file's scene
    //   name / original path / path using a frozen copy of the source-token
    //   rules (a migration must not depend on the live parser), falling back
    //   to Archive for comic archives. Archive-vs-Archive never triggers an
    //   upgrade, so this causes no re-download churn.
    // - QualityDefinitions reconcile themselves on startup.
    [Migration(10)]
    public class SourceBasedQualities : NzbDroneMigrationBase
    {
        private const int Pdf = 1;
        private const int Epub = 2;
        private const int Scan = 8;
        private const int C2c = 9;
        private const int Archive = 10;
        private const int WebRip = 11;
        private const int Digital = 12;

        private static readonly HashSet<int> RetiredContainerIds = new () { 3, 4, 5, 6, 7 };

        private static readonly Regex DigitalRegex = new (@"\bdigital\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WebRipRegex = new (@"\bweb[-_. ]?rip\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex C2cRegex = new (@"\b(?:c2c|cover[ ._-]to[ ._-]cover)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ScanRegex = new (@"\b(?:scan(?:ned)?|print)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PdfRegex = new (@"\.pdf$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex EpubRegex = new (@"\.(?:epub|mobi|azw3?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Weight-preserving map for display-only rows (history, blocklist,
        // pending releases): CBR(30)->Scan, CBZ Web(35)->C2C, CBZ(40)->Archive,
        // CB7(45)->WebRip, CBZ HD(50)->Digital. Quality.FindById throws on
        // retired ids, so these rows cannot simply be left behind.
        private static readonly Dictionary<int, int> WeightMap = new ()
        {
            { 3, Scan },
            { 6, C2c },
            { 4, Archive },
            { 5, WebRip },
            { 7, Digital }
        };

        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(MigrateProfiles);
            Execute.WithConnection(MigrateComicFiles);
            Execute.WithConnection((conn, tran) => MigrateQualityModelColumn(conn, tran, "History", "Quality"));
            Execute.WithConnection((conn, tran) => MigrateQualityModelColumn(conn, tran, "Blocklist", "Quality"));
            Execute.WithConnection(MigratePendingReleases);

            // The startup reconciler cannot delete these itself: loading a row
            // with a retired quality id throws before it gets the chance.
            Execute.Sql("DELETE FROM \"QualityDefinitions\" WHERE \"Quality\" IN (3, 4, 5, 6, 7)");
        }

        private void MigrateProfiles(IDbConnection conn, IDbTransaction tran)
        {
            var profiles = conn.Query<(int Id, int Cutoff, string Items)>(
                "SELECT \"Id\", \"Cutoff\", \"Items\" FROM \"QualityProfiles\"", transaction: tran);

            foreach (var profile in profiles)
            {
                var items = JArray.Parse(profile.Items);

                var anyContainerAllowed = false;
                var pdfAllowed = false;
                var epubAllowed = false;
                var unknownAllowed = false;

                foreach (var item in items)
                {
                    var quality = item.Value<int>("quality");
                    var allowed = item.Value<bool>("allowed");

                    if (RetiredContainerIds.Contains(quality) && allowed)
                    {
                        anyContainerAllowed = true;
                    }
                    else if (quality == Pdf)
                    {
                        pdfAllowed = allowed;
                    }
                    else if (quality == Epub)
                    {
                        epubAllowed = allowed;
                    }
                    else if (quality == 0)
                    {
                        unknownAllowed = allowed;
                    }
                }

                var newItems = new JArray
                {
                    ProfileItem(0, unknownAllowed),
                    ProfileItem(Pdf, pdfAllowed),
                    ProfileItem(Epub, epubAllowed),
                    ProfileItem(Scan, anyContainerAllowed),
                    ProfileItem(C2c, anyContainerAllowed),
                    ProfileItem(Archive, anyContainerAllowed),
                    ProfileItem(WebRip, anyContainerAllowed),
                    ProfileItem(Digital, anyContainerAllowed)
                };

                var newCutoff = RetiredContainerIds.Contains(profile.Cutoff) ? Digital : profile.Cutoff;

                conn.Execute("UPDATE \"QualityProfiles\" SET \"Cutoff\" = @Cutoff, \"Items\" = @Items WHERE \"Id\" = @Id",
                             new { Id = profile.Id, Cutoff = newCutoff, Items = newItems.ToString() },
                             transaction: tran);
            }
        }

        private static JObject ProfileItem(int quality, bool allowed)
        {
            return new JObject
            {
                ["quality"] = quality,
                ["items"] = new JArray(),
                ["allowed"] = allowed
            };
        }

        private void MigrateComicFiles(IDbConnection conn, IDbTransaction tran)
        {
            var files = conn.Query<(long Id, string Quality, string SceneName, string OriginalFilePath, string Path)>(
                "SELECT \"Id\", \"Quality\", \"SceneName\", \"OriginalFilePath\", \"Path\" FROM \"ComicFiles\"", transaction: tran);

            foreach (var file in files)
            {
                if (string.IsNullOrWhiteSpace(file.Quality))
                {
                    continue;
                }

                var quality = JObject.Parse(file.Quality);
                var qualityId = quality.Value<int>("quality");

                if (!RetiredContainerIds.Contains(qualityId))
                {
                    continue;
                }

                var name = !string.IsNullOrWhiteSpace(file.SceneName) ? file.SceneName
                    : !string.IsNullOrWhiteSpace(file.OriginalFilePath) ? file.OriginalFilePath
                    : file.Path ?? string.Empty;

                quality["quality"] = DeriveSourceQuality(name, file.Path ?? string.Empty);

                conn.Execute("UPDATE \"ComicFiles\" SET \"Quality\" = @Quality WHERE \"Id\" = @Id",
                             new { Id = file.Id, Quality = quality.ToString() },
                             transaction: tran);
            }
        }

        private void MigrateQualityModelColumn(IDbConnection conn, IDbTransaction tran, string table, string column)
        {
            var rows = conn.Query<(long Id, string Quality)>(
                $"SELECT \"Id\", \"{column}\" FROM \"{table}\"", transaction: tran);

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Quality))
                {
                    continue;
                }

                var quality = JObject.Parse(row.Quality);

                if (!WeightMap.TryGetValue(quality.Value<int>("quality"), out var newId))
                {
                    continue;
                }

                quality["quality"] = newId;

                conn.Execute($"UPDATE \"{table}\" SET \"{column}\" = @Quality WHERE \"Id\" = @Id",
                             new { Id = row.Id, Quality = quality.ToString() },
                             transaction: tran);
            }
        }

        private void MigratePendingReleases(IDbConnection conn, IDbTransaction tran)
        {
            var rows = conn.Query<(long Id, string ParsedIssueInfo)>(
                "SELECT \"Id\", \"ParsedIssueInfo\" FROM \"PendingReleases\"", transaction: tran);

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.ParsedIssueInfo))
                {
                    continue;
                }

                var info = JObject.Parse(row.ParsedIssueInfo);
                var quality = info["quality"] as JObject;

                if (quality == null || !WeightMap.TryGetValue(quality.Value<int>("quality"), out var newId))
                {
                    continue;
                }

                quality["quality"] = newId;

                conn.Execute("UPDATE \"PendingReleases\" SET \"ParsedIssueInfo\" = @Info WHERE \"Id\" = @Id",
                             new { Id = row.Id, Info = info.ToString() },
                             transaction: tran);
            }
        }

        private static int DeriveSourceQuality(string name, string path)
        {
            if (PdfRegex.IsMatch(path))
            {
                return Pdf;
            }

            if (EpubRegex.IsMatch(path))
            {
                return Epub;
            }

            if (DigitalRegex.IsMatch(name))
            {
                return Digital;
            }

            if (WebRipRegex.IsMatch(name))
            {
                return WebRip;
            }

            if (C2cRegex.IsMatch(name))
            {
                return C2c;
            }

            if (ScanRegex.IsMatch(name))
            {
                return Scan;
            }

            return Archive;
        }
    }
}
