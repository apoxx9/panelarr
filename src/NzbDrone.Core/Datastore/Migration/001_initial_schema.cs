using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(1)]
    public class InitialSchema : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // ── Core domain tables ────────────────────────────────────────────
            Create.TableForModel("Publishers")
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("CleanName").AsString().NotNullable()
                .WithColumn("ForeignPublisherId").AsString().Unique().Nullable()
                .WithColumn("Description").AsString().Nullable()
                .WithColumn("Images").AsString().Nullable();

            Create.TableForModel("SeriesMetadata")
                .WithColumn("ForeignSeriesId").AsString().NotNullable().Unique()
                .WithColumn("TitleSlug").AsString().NotNullable().Unique()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("SortName").AsString().NotNullable()
                .WithColumn("Year").AsInt32().Nullable()
                .WithColumn("Overview").AsString().Nullable()
                .WithColumn("Status").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("SeriesType").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("VolumeNumber").AsInt32().Nullable()
                .WithColumn("PublisherId").AsInt32().Nullable()
                .WithColumn("Images").AsString().Nullable()
                .WithColumn("Links").AsString().Nullable()
                .WithColumn("Genres").AsString().Nullable()
                .WithColumn("Ratings").AsString().Nullable();

            Create.TableForModel("Series")
                .WithColumn("SeriesMetadataId").AsInt32().NotNullable().Unique()
                .WithColumn("CleanName").AsString().NotNullable().Indexed()
                .WithColumn("Path").AsString().Nullable().Indexed()
                .WithColumn("RootFolderPath").AsString().Nullable()
                .WithColumn("Monitored").AsBoolean().NotNullable()
                .WithColumn("MonitorNewItems").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("LastInfoSync").AsDateTimeOffset().Nullable()
                .WithColumn("QualityProfileId").AsInt32().NotNullable()
                .WithColumn("Tags").AsString().Nullable()
                .WithColumn("Added").AsDateTimeOffset().Nullable()
                .WithColumn("AddOptions").AsString().Nullable();

            Create.Index().OnTable("Series").OnColumn("Monitored").Ascending();

            Create.TableForModel("Issues")
                .WithColumn("SeriesMetadataId").AsInt32().NotNullable()
                .WithColumn("ForeignIssueId").AsString().NotNullable().Unique()
                .WithColumn("TitleSlug").AsString().NotNullable().Unique()
                .WithColumn("Title").AsString().Nullable()
                .WithColumn("IssueNumber").AsFloat().NotNullable()
                .WithColumn("IssueType").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("ReleaseDate").AsDateTimeOffset().Nullable()
                .WithColumn("CoverArtUrl").AsString().Nullable()
                .WithColumn("PageCount").AsInt32().Nullable()
                .WithColumn("CleanTitle").AsString().Nullable().Indexed()
                .WithColumn("Monitored").AsBoolean().NotNullable()
                .WithColumn("LastInfoSync").AsDateTimeOffset().Nullable()
                .WithColumn("LastSearchTime").AsDateTimeOffset().Nullable()
                .WithColumn("Added").AsDateTimeOffset().Nullable()
                .WithColumn("AddOptions").AsString().Nullable()
                .WithColumn("Links").AsString().Nullable()
                .WithColumn("Genres").AsString().Nullable()
                .WithColumn("Ratings").AsString().Nullable();

            Create.Index().OnTable("Issues").OnColumn("SeriesMetadataId").Ascending();
            Create.Index().OnTable("Issues").OnColumn("SeriesMetadataId").Ascending()
                .OnColumn("ReleaseDate").Ascending();

            Create.TableForModel("SeriesGroup")
                .WithColumn("ForeignSeriesGroupId").AsString().Nullable().Unique()
                .WithColumn("Title").AsString().NotNullable()
                .WithColumn("Description").AsString().Nullable()
                .WithColumn("SortTitle").AsString().Nullable();

            Create.TableForModel("SeriesGroupLink")
                .WithColumn("SeriesGroupId").AsInt32().NotNullable().Indexed()
                    .ForeignKey("SeriesGroup", "Id").OnDelete(Rule.Cascade)
                .WithColumn("SeriesMetadataId").AsInt32().NotNullable().Indexed()
                .WithColumn("Position").AsString().Nullable()
                .WithColumn("SeriesPosition").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("IsPrimary").AsBoolean().NotNullable().WithDefaultValue(true);

            Create.TableForModel("ComicFiles")
                .WithColumn("IssueId").AsInt32().NotNullable().Indexed()
                .WithColumn("Path").AsString().NotNullable().Unique()
                .WithColumn("Size").AsInt64().NotNullable()
                .WithColumn("Modified").AsDateTimeOffset().NotNullable()
                .WithColumn("DateAdded").AsDateTimeOffset().NotNullable()
                .WithColumn("OriginalFilePath").AsString().Nullable()
                .WithColumn("SceneName").AsString().Nullable()
                .WithColumn("ReleaseGroup").AsString().Nullable()
                .WithColumn("Quality").AsString().NotNullable()
                .WithColumn("IndexerFlags").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("MediaInfo").AsString().Nullable()
                .WithColumn("Part").AsInt32().NotNullable().WithDefaultValue(1)
                .WithColumn("ComicFormat").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("ImageCount").AsInt32().Nullable()
                .WithColumn("ImageQualityScore").AsFloat().Nullable();

            // ── Inherited infrastructure tables ───────────────────────────────
            Create.TableForModel("Config")
                .WithColumn("Key").AsString().NotNullable().Unique()
                .WithColumn("Value").AsString().NotNullable();

            Create.TableForModel("RootFolders")
                .WithColumn("Path").AsString().NotNullable().Unique()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("DefaultMetadataProfileId").AsInt32().WithDefaultValue(0)
                .WithColumn("DefaultQualityProfileId").AsInt32().WithDefaultValue(0)
                .WithColumn("DefaultMonitorOption").AsInt32().WithDefaultValue(0)
                .WithColumn("DefaultTags").AsString().Nullable()
                .WithColumn("IsCalibreLibrary").AsBoolean().WithDefaultValue(false)
                .WithColumn("CalibreSettings").AsString().Nullable()
                .WithColumn("DefaultNewItemMonitorOption").AsInt32().WithDefaultValue(0);

            Create.TableForModel("History")
                .WithColumn("SourceTitle").AsString()
                .WithColumn("Date").AsDateTimeOffset().Indexed()
                .WithColumn("Quality").AsString()
                .WithColumn("Data").AsString()
                .WithColumn("EventType").AsInt32().Nullable().Indexed()
                .WithColumn("DownloadId").AsString().Nullable().Indexed()
                .WithColumn("SeriesId").AsInt32().WithDefaultValue(0)
                .WithColumn("IssueId").AsInt32().WithDefaultValue(0).Indexed();

            Delete.Index().OnTable("History").OnColumn("IssueId");
            Create.Index().OnTable("History").OnColumn("IssueId").Ascending()
                .OnColumn("Date").Descending();

            Delete.Index().OnTable("History").OnColumn("DownloadId");
            Create.Index().OnTable("History").OnColumn("DownloadId").Ascending()
                .OnColumn("Date").Descending();

            Create.TableForModel("Notifications")
                .WithColumn("Name").AsString()
                .WithColumn("OnGrab").AsBoolean()
                .WithColumn("Settings").AsString()
                .WithColumn("Implementation").AsString()
                .WithColumn("ConfigContract").AsString().Nullable()
                .WithColumn("OnUpgrade").AsBoolean().Nullable()
                .WithColumn("Tags").AsString().Nullable()
                .WithColumn("OnRename").AsBoolean().NotNullable()
                .WithColumn("OnReleaseImport").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnHealthIssue").AsBoolean().WithDefaultValue(false)
                .WithColumn("IncludeHealthWarnings").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnDownloadFailure").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnImportFailure").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnIssueRetag").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnSeriesDelete").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnIssueDelete").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnComicFileDelete").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnComicFileDeleteForUpgrade").AsBoolean().WithDefaultValue(false)
                .WithColumn("OnApplicationUpdate").AsBoolean().WithDefaultValue(true)
                .WithColumn("OnSeriesAdded").AsBoolean().WithDefaultValue(false);

            Create.TableForModel("ScheduledTasks")
                .WithColumn("TypeName").AsString().Unique()
                .WithColumn("Interval").AsInt32()
                .WithColumn("LastExecution").AsDateTime()
                .WithColumn("LastStartTime").AsDateTime().Nullable();

            Create.TableForModel("Indexers")
                .WithColumn("Name").AsString().NotNullable().Unique()
                .WithColumn("Implementation").AsString()
                .WithColumn("Settings").AsString().Nullable()
                .WithColumn("ConfigContract").AsString().Nullable()
                .WithColumn("EnableRss").AsBoolean().Nullable()
                .WithColumn("EnableAutomaticSearch").AsBoolean().Nullable()
                .WithColumn("EnableInteractiveSearch").AsBoolean().NotNullable()
                .WithColumn("Priority").AsInt32().NotNullable().WithDefaultValue(25)
                .WithColumn("Tags").AsString().Nullable()
                .WithColumn("DownloadClientId").AsInt32().WithDefaultValue(0);

            Create.TableForModel("QualityProfiles")
                .WithColumn("Name").AsString().Unique()
                .WithColumn("Cutoff").AsInt32()
                .WithColumn("Items").AsString().NotNullable()
                .WithColumn("UpgradeAllowed").AsBoolean().Nullable()
                .WithColumn("FormatItems").AsString().WithDefaultValue("[]")
                .WithColumn("MinFormatScore").AsInt32().WithDefaultValue(0)
                .WithColumn("CutoffFormatScore").AsInt32().WithDefaultValue(0);

            Create.TableForModel("MetadataProfiles")
                .WithColumn("Name").AsString().Unique()
                .WithColumn("MinPopularity").AsDouble()
                .WithColumn("SkipMissingDate").AsBoolean()
                .WithColumn("SkipMissingIsbn").AsBoolean()
                .WithColumn("SkipPartsAndSets").AsBoolean()
                .WithColumn("SkipSeriesSecondary").AsBoolean()
                .WithColumn("AllowedLanguages").AsString().Nullable()
                .WithColumn("MinPages").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("Ignored").AsString().Nullable();

            Create.TableForModel("QualityDefinitions")
                .WithColumn("Quality").AsInt32().Unique()
                .WithColumn("Title").AsString().Unique()
                .WithColumn("MinSize").AsDouble().Nullable()
                .WithColumn("MaxSize").AsDouble().Nullable();

            Create.TableForModel("NamingConfig")
                .WithColumn("ReplaceIllegalCharacters").AsBoolean().WithDefaultValue(true)
                .WithColumn("ColonReplacementFormat").AsInt32().WithDefaultValue(4)
                .WithColumn("RenameComics").AsBoolean().Nullable()
                .WithColumn("StandardIssueFormat").AsString().Nullable()
                .WithColumn("AnnualIssueFormat").AsString().Nullable()
                .WithColumn("TPBFormat").AsString().Nullable()
                .WithColumn("SeriesFolderFormat").AsString().Nullable();

            Create.TableForModel("Blocklist")
                .WithColumn("SourceTitle").AsString()
                .WithColumn("Quality").AsString()
                .WithColumn("Date").AsDateTimeOffset().NotNullable()
                .WithColumn("PublishedDate").AsDateTimeOffset().Nullable()
                .WithColumn("Size").AsInt64().Nullable()
                .WithColumn("Protocol").AsInt32().Nullable()
                .WithColumn("Indexer").AsString().Nullable()
                .WithColumn("Message").AsString().Nullable()
                .WithColumn("TorrentInfoHash").AsString().Nullable()
                .WithColumn("SeriesId").AsInt32().WithDefaultValue(0)
                .WithColumn("IssueIds").AsString().WithDefaultValue("")
                .WithColumn("IndexerFlags").AsInt32().WithDefaultValue(0);

            Create.TableForModel("Metadata")
                .WithColumn("Enable").AsBoolean().NotNullable()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Implementation").AsString().NotNullable()
                .WithColumn("Settings").AsString().NotNullable()
                .WithColumn("ConfigContract").AsString().NotNullable();

            Create.TableForModel("MetadataFiles")
                .WithColumn("SeriesId").AsInt32().NotNullable()
                .WithColumn("Consumer").AsString().NotNullable()
                .WithColumn("Type").AsInt32().NotNullable()
                .WithColumn("RelativePath").AsString().NotNullable()
                .WithColumn("LastUpdated").AsDateTime().NotNullable()
                .WithColumn("IssueId").AsInt32().Nullable()
                .WithColumn("ComicFileId").AsInt32().Nullable()
                .WithColumn("Hash").AsString().Nullable()
                .WithColumn("Added").AsDateTime().Nullable()
                .WithColumn("Extension").AsString().NotNullable();

            Create.TableForModel("DownloadClients")
                .WithColumn("Enable").AsBoolean().NotNullable()
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Implementation").AsString().NotNullable()
                .WithColumn("Settings").AsString().NotNullable()
                .WithColumn("ConfigContract").AsString().NotNullable()
                .WithColumn("Priority").AsInt32().WithDefaultValue(1)
                .WithColumn("RemoveCompletedDownloads").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("RemoveFailedDownloads").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("Tags").AsString().Nullable();

            Create.TableForModel("PendingReleases")
                .WithColumn("Title").AsString()
                .WithColumn("Added").AsDateTime()
                .WithColumn("Release").AsString()
                .WithColumn("SeriesId").AsInt32().WithDefaultValue(0)
                .WithColumn("ParsedIssueInfo").AsString().WithDefaultValue("")
                .WithColumn("Reason").AsInt32().WithDefaultValue(0);

            Create.TableForModel("RemotePathMappings")
                .WithColumn("Host").AsString()
                .WithColumn("RemotePath").AsString()
                .WithColumn("LocalPath").AsString();

            Create.TableForModel("Tags")
                .WithColumn("Label").AsString().Unique();

            Create.TableForModel("ReleaseProfiles")
                .WithColumn("Required").AsString().Nullable()
                .WithColumn("Preferred").AsString().Nullable()
                .WithColumn("Ignored").AsString().Nullable()
                .WithColumn("Tags").AsString().NotNullable()
                .WithColumn("IncludePreferredWhenRenaming").AsBoolean().WithDefaultValue(true)
                .WithColumn("Enabled").AsBoolean().WithDefaultValue(true)
                .WithColumn("IndexerId").AsInt32().WithDefaultValue(0);

            Create.TableForModel("DelayProfiles")
                .WithColumn("EnableUsenet").AsBoolean().NotNullable()
                .WithColumn("EnableTorrent").AsBoolean().NotNullable()
                .WithColumn("PreferredProtocol").AsInt32().NotNullable()
                .WithColumn("UsenetDelay").AsInt32().NotNullable()
                .WithColumn("TorrentDelay").AsInt32().NotNullable()
                .WithColumn("Order").AsInt32().NotNullable()
                .WithColumn("Tags").AsString().NotNullable()
                .WithColumn("BypassIfHighestQuality").AsBoolean().WithDefaultValue(false)
                .WithColumn("BypassIfAboveCustomFormatScore").AsBoolean().WithDefaultValue(false)
                .WithColumn("MinimumCustomFormatScore").AsInt32().Nullable();

            Create.TableForModel("Users")
                .WithColumn("Identifier").AsString().NotNullable().Unique()
                .WithColumn("Username").AsString().NotNullable().Unique()
                .WithColumn("Password").AsString().NotNullable();

            Create.TableForModel("Commands")
                .WithColumn("Name").AsString().NotNullable()
                .WithColumn("Body").AsString().NotNullable()
                .WithColumn("Priority").AsInt32().NotNullable()
                .WithColumn("Status").AsInt32().NotNullable()
                .WithColumn("QueuedAt").AsDateTimeOffset().NotNullable()
                .WithColumn("StartedAt").AsDateTimeOffset().Nullable()
                .WithColumn("EndedAt").AsDateTimeOffset().Nullable()
                .WithColumn("Duration").AsString().Nullable()
                .WithColumn("Exception").AsString().Nullable()
                .WithColumn("Trigger").AsInt32().NotNullable()
                .WithColumn("Result").AsInt32().WithDefaultValue(1);

            Create.TableForModel("IndexerStatus")
                .WithColumn("ProviderId").AsInt32().NotNullable().Unique()
                .WithColumn("InitialFailure").AsDateTime().Nullable()
                .WithColumn("MostRecentFailure").AsDateTime().Nullable()
                .WithColumn("EscalationLevel").AsInt32().NotNullable()
                .WithColumn("DisabledTill").AsDateTime().Nullable()
                .WithColumn("LastRssSyncReleaseInfo").AsString().Nullable();

            Create.TableForModel("ExtraFiles")
                .WithColumn("SeriesId").AsInt32().NotNullable()
                .WithColumn("IssueId").AsInt32().NotNullable()
                .WithColumn("ComicFileId").AsInt32().NotNullable()
                .WithColumn("RelativePath").AsString().NotNullable()
                .WithColumn("Extension").AsString().NotNullable()
                .WithColumn("Added").AsDateTimeOffset().NotNullable()
                .WithColumn("LastUpdated").AsDateTimeOffset().NotNullable();

            Create.TableForModel("DownloadClientStatus")
                .WithColumn("ProviderId").AsInt32().NotNullable().Unique()
                .WithColumn("InitialFailure").AsDateTimeOffset().Nullable()
                .WithColumn("MostRecentFailure").AsDateTimeOffset().Nullable()
                .WithColumn("EscalationLevel").AsInt32().NotNullable()
                .WithColumn("DisabledTill").AsDateTimeOffset().Nullable();

            Create.TableForModel("ImportLists")
                .WithColumn("Name").AsString().Unique()
                .WithColumn("Implementation").AsString()
                .WithColumn("Settings").AsString().Nullable()
                .WithColumn("ConfigContract").AsString().Nullable()
                .WithColumn("EnableAutomaticAdd").AsBoolean().Nullable()
                .WithColumn("RootFolderPath").AsString()
                .WithColumn("ShouldMonitor").AsInt32()
                .WithColumn("ProfileId").AsInt32()
                .WithColumn("MetadataProfileId").AsInt32()
                .WithColumn("Tags").AsString().Nullable()
                .WithColumn("ShouldSearch").AsBoolean().WithDefaultValue(true)
                .WithColumn("ShouldMonitorExisting").AsBoolean().WithDefaultValue(false)
                .WithColumn("MonitorNewItems").AsInt32().WithDefaultValue(0);

            Create.TableForModel("ImportListStatus")
                .WithColumn("ProviderId").AsInt32().NotNullable().Unique()
                .WithColumn("InitialFailure").AsDateTime().Nullable()
                .WithColumn("MostRecentFailure").AsDateTime().Nullable()
                .WithColumn("EscalationLevel").AsInt32().NotNullable()
                .WithColumn("DisabledTill").AsDateTime().Nullable()
                .WithColumn("LastSyncListInfo").AsString().Nullable()
                .WithColumn("LastInfoSync").AsDateTimeOffset().Nullable();

            Create.TableForModel("ImportListExclusions")
                .WithColumn("ForeignId").AsString().NotNullable().Unique()
                .WithColumn("Name").AsString().NotNullable();

            Create.TableForModel("CustomFilters")
                .WithColumn("Type").AsString().NotNullable()
                .WithColumn("Label").AsString().NotNullable()
                .WithColumn("Filters").AsString().NotNullable();

            Create.TableForModel("DownloadHistory")
                .WithColumn("EventType").AsInt32().NotNullable()
                .WithColumn("SeriesId").AsInt32().NotNullable()
                .WithColumn("DownloadId").AsString().NotNullable()
                .WithColumn("SourceTitle").AsString().NotNullable()
                .WithColumn("Date").AsDateTimeOffset().Nullable()
                .WithColumn("Protocol").AsInt32().Nullable()
                .WithColumn("IndexerId").AsInt32().Nullable()
                .WithColumn("DownloadClientId").AsInt32().Nullable()
                .WithColumn("Release").AsString().Nullable()
                .WithColumn("Data").AsString().Nullable();

            Create.TableForModel("UpdateHistory")
                .WithColumn("Date").AsDateTime().NotNullable().Indexed()
                .WithColumn("Version").AsString().NotNullable()
                .WithColumn("EventType").AsInt32().NotNullable();

            Create.TableForModel("CustomFormats")
                .WithColumn("Name").AsString().Unique()
                .WithColumn("Specifications").AsString().WithDefaultValue("[]")
                .WithColumn("IncludeCustomFormatWhenRenaming").AsBoolean().WithDefaultValue(false);

            Create.TableForModel("NotificationStatus")
                .WithColumn("ProviderId").AsInt32().NotNullable().Unique()
                .WithColumn("InitialFailure").AsDateTimeOffset().Nullable()
                .WithColumn("MostRecentFailure").AsDateTimeOffset().Nullable()
                .WithColumn("EscalationLevel").AsInt32().NotNullable()
                .WithColumn("DisabledTill").AsDateTimeOffset().Nullable();

            // ── Seed data ─────────────────────────────────────────────────────
            Insert.IntoTable("DelayProfiles").Row(new
            {
                EnableUsenet = true,
                EnableTorrent = true,
                PreferredProtocol = 1,
                UsenetDelay = 0,
                TorrentDelay = 0,
                Order = int.MaxValue,
                Tags = "[]",
                BypassIfHighestQuality = false,
                BypassIfAboveCustomFormatScore = false
            });
        }

        protected override void LogDbUpgrade()
        {
            Create.TableForModel("Logs")
                .WithColumn("Message").AsString()
                .WithColumn("Time").AsDateTime().Indexed()
                .WithColumn("Logger").AsString()
                .WithColumn("Exception").AsString().Nullable()
                .WithColumn("ExceptionType").AsString().Nullable()
                .WithColumn("Level").AsString();
        }

        protected override void CacheDbUpgrade()
        {
            Create.TableForModel("HttpResponse")
                .WithColumn("Url").AsString().Indexed()
                .WithColumn("LastRefresh").AsDateTime()
                .WithColumn("Expiry").AsDateTime().Indexed()
                .WithColumn("Value").AsString()
                .WithColumn("StatusCode").AsInt32();
        }
    }
}
