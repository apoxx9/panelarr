using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Common.Reflection;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFilters;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.History;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.History;
using NzbDrone.Core.Http;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Instrumentation;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Profiles.Releases;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RemotePathMappings;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Tags;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Update.History;
using static Dapper.SqlMapper;

namespace NzbDrone.Core.Datastore
{
    public static class TableMapping
    {
        static TableMapping()
        {
            Mapper = new TableMapper();
        }

        public static TableMapper Mapper { get; private set; }

        public static void Map()
        {
            RegisterMappers();

            Mapper.Entity<Config>("Config").RegisterModel();

            Mapper.Entity<RootFolder>("RootFolders").RegisterModel()
                  .Ignore(r => r.Accessible)
                  .Ignore(r => r.FreeSpace)
                  .Ignore(r => r.TotalSpace);

            Mapper.Entity<ScheduledTask>("ScheduledTasks").RegisterModel()
                  .Ignore(i => i.Priority);

            Mapper.Entity<IndexerDefinition>("Indexers").RegisterModel()
                  .Ignore(x => x.ImplementationName)
                  .Ignore(i => i.Enable)
                  .Ignore(i => i.Protocol)
                  .Ignore(i => i.SupportsRss)
                  .Ignore(i => i.SupportsSearch);

            Mapper.Entity<ImportListDefinition>("ImportLists").RegisterModel()
                  .Ignore(x => x.ImplementationName)
                  .Ignore(i => i.ListType)
                  .Ignore(i => i.MinRefreshInterval)
                  .Ignore(i => i.Enable);

            Mapper.Entity<NotificationDefinition>("Notifications").RegisterModel()
                  .Ignore(x => x.ImplementationName)
                  .Ignore(i => i.SupportsOnGrab)
                  .Ignore(i => i.SupportsOnReleaseImport)
                  .Ignore(i => i.SupportsOnUpgrade)
                  .Ignore(i => i.SupportsOnRename)
                  .Ignore(i => i.SupportsOnSeriesAdded)
                  .Ignore(i => i.SupportsOnSeriesDelete)
                  .Ignore(i => i.SupportsOnIssueDelete)
                  .Ignore(i => i.SupportsOnComicFileDelete)
                  .Ignore(i => i.SupportsOnComicFileDeleteForUpgrade)
                  .Ignore(i => i.SupportsOnHealthIssue)
                  .Ignore(i => i.SupportsOnDownloadFailure)
                  .Ignore(i => i.SupportsOnImportFailure)
                  .Ignore(i => i.SupportsOnIssueRetag)
                  .Ignore(i => i.SupportsOnApplicationUpdate);

            Mapper.Entity<MetadataDefinition>("Metadata").RegisterModel()
                  .Ignore(x => x.ImplementationName)
                  .Ignore(d => d.Tags);

            Mapper.Entity<DownloadClientDefinition>("DownloadClients").RegisterModel()
                  .Ignore(x => x.ImplementationName)
                  .Ignore(d => d.Protocol);

            Mapper.Entity<EntityHistory>("History").RegisterModel();

            Mapper.Entity<Series>("Series")
                  .Ignore(s => s.RootFolderPath)
                  .Ignore(s => s.Name)
                  .Ignore(s => s.ForeignSeriesId)
                  .HasOne(a => a.Metadata, a => a.SeriesMetadataId)
                  .HasOne(a => a.QualityProfile, a => a.QualityProfileId)
                  .LazyLoad(a => a.Issues, (db, a) => db.Query<Issue>(new SqlBuilder(db.DatabaseType).Where<Issue>(b => b.SeriesMetadataId == a.SeriesMetadataId)).ToList(), a => a.SeriesMetadataId > 0);

            Mapper.Entity<SeriesGroup>("SeriesGroup").RegisterModel()
                .Ignore(s => s.ForeignSeriesId)
                .Ignore(s => s.Numbered)
                .Ignore(s => s.WorkCount)
                .Ignore(s => s.PrimaryWorkCount)
                .LazyLoad(s => s.LinkItems,
                          (db, series) => db.Query<SeriesGroupLink>(new SqlBuilder(db.DatabaseType).Where<SeriesGroupLink>(s => s.SeriesGroupId == series.Id)).ToList(),
                          s => s.Id > 0)
                .LazyLoad(s => s.Issues,
                          (db, series) => db.Query<Issue>(new SqlBuilder(db.DatabaseType)
                                                         .Join<Issue, SeriesGroupLink>((l, r) => l.SeriesMetadataId == r.SeriesMetadataId)
                                                         .Join<SeriesGroupLink, SeriesGroup>((l, r) => l.SeriesGroupId == r.Id)
                                                         .Where<SeriesGroup>(s => s.Id == series.Id)).ToList(),
                          s => s.Id > 0);

            Mapper.Entity<SeriesGroupLink>("SeriesGroupLink").RegisterModel()
                  .HasOne(l => l.Issue, l => l.SeriesMetadataId)
                  .HasOne(l => l.SeriesGroup, l => l.SeriesGroupId);

            Mapper.Entity<SeriesMetadata>("SeriesMetadata").RegisterModel();

            Mapper.Entity<Issue>("Issues").RegisterModel()
                .Ignore(x => x.SeriesId)
                .HasOne(r => r.SeriesMetadata, r => r.SeriesMetadataId)
                .LazyLoad(x => x.ComicFiles,
                          (db, issue) => db.Query<ComicFile>(new SqlBuilder(db.DatabaseType)
                                                           .Where<ComicFile>(f => f.IssueId == issue.Id)).ToList(),
                          b => b.Id > 0)
                .LazyLoad(a => a.Series,
                          (db, issue) => SeriesRepository.Query(db,
                                                                new SqlBuilder(db.DatabaseType)
                                                                .Join<Series, SeriesMetadata>((a, m) => a.SeriesMetadataId == m.Id)
                                                                .Where<Series>(a => a.SeriesMetadataId == issue.SeriesMetadataId)).SingleOrDefault(),
                          a => a.SeriesMetadataId > 0)
                .LazyLoad(b => b.SeriesLinks,
                          (db, issue) => db.Query<SeriesGroupLink>(new SqlBuilder(db.DatabaseType).Where<SeriesGroupLink>(s => s.SeriesMetadataId == issue.SeriesMetadataId)).ToList(),
                          b => b.Id > 0);

            Mapper.Entity<ComicFile>("ComicFiles").RegisterModel()
                .Ignore(x => x.PartCount)
                .HasOne(f => f.Issue, f => f.IssueId)
                .LazyLoad(x => x.Series,
                          (db, f) => SeriesRepository.Query(db,
                                                            new SqlBuilder(db.DatabaseType)
                                                            .Join<Series, SeriesMetadata>((a, m) => a.SeriesMetadataId == m.Id)
                                                            .Join<Series, Issue>((l, r) => l.SeriesMetadataId == r.SeriesMetadataId)
                                                            .Where<Issue>(a => a.Id == f.IssueId)).SingleOrDefault(),
                          t => t.Id > 0);

            Mapper.Entity<QualityDefinition>("QualityDefinitions").RegisterModel()
                  .Ignore(d => d.GroupName)
                  .Ignore(d => d.GroupWeight)
                  .Ignore(d => d.Weight);

            Mapper.Entity<CustomFormat>("CustomFormats").RegisterModel();

            Mapper.Entity<QualityProfile>("QualityProfiles").RegisterModel();
            Mapper.Entity<MetadataProfile>("MetadataProfiles").RegisterModel();
            Mapper.Entity<Log>("Logs").RegisterModel();
            Mapper.Entity<NamingConfig>("NamingConfig").RegisterModel();

            Mapper.Entity<Blocklist>("Blocklist").RegisterModel();
            Mapper.Entity<MetadataFile>("MetadataFiles").RegisterModel();
            Mapper.Entity<OtherExtraFile>("ExtraFiles").RegisterModel();

            Mapper.Entity<PendingRelease>("PendingReleases").RegisterModel()
                  .Ignore(e => e.RemoteIssue);

            Mapper.Entity<RemotePathMapping>("RemotePathMappings").RegisterModel();
            Mapper.Entity<Tag>("Tags").RegisterModel();
            Mapper.Entity<ReleaseProfile>("ReleaseProfiles").RegisterModel();

            Mapper.Entity<DelayProfile>("DelayProfiles").RegisterModel();
            Mapper.Entity<User>("Users").RegisterModel();
            Mapper.Entity<CommandModel>("Commands").RegisterModel()
                  .Ignore(c => c.Message);

            Mapper.Entity<IndexerStatus>("IndexerStatus").RegisterModel();
            Mapper.Entity<DownloadClientStatus>("DownloadClientStatus").RegisterModel();
            Mapper.Entity<ImportListStatus>("ImportListStatus").RegisterModel();
            Mapper.Entity<NotificationStatus>("NotificationStatus").RegisterModel();

            Mapper.Entity<CustomFilter>("CustomFilters").RegisterModel();
            Mapper.Entity<ImportListExclusion>("ImportListExclusions").RegisterModel();

            Mapper.Entity<CachedHttpResponse>("HttpResponse").RegisterModel();

            Mapper.Entity<DownloadHistory>("DownloadHistory").RegisterModel();

            Mapper.Entity<UpdateHistory>("UpdateHistory").RegisterModel();

            Mapper.Entity<Publisher>("Publishers").RegisterModel();
        }

        private static void RegisterMappers()
        {
            RegisterEmbeddedConverter();
            RegisterProviderSettingConverter();

            SqlMapper.RemoveTypeMap(typeof(DateTime));
            SqlMapper.AddTypeHandler(new DapperUtcConverter());
            SqlMapper.AddTypeHandler(new DapperTimeSpanConverter());
            SqlMapper.AddTypeHandler(new DapperQualityIntConverter());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<QualityProfileQualityItem>>(new QualityIntConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<ProfileFormatItem>>(new CustomFormatIntConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<ICustomFormatSpecification>>(new CustomFormatSpecificationListConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<QualityModel>(new QualityIntConverter()));
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<Dictionary<string, string>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<IDictionary<string, string>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<int>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<KeyValuePair<string, int>>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<KeyValuePair<string, int>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<string>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<ParsedIssueInfo>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<ParsedTrackInfo>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<ReleaseInfo>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<HashSet<int>>());
            SqlMapper.AddTypeHandler(new OsPathConverter());
            SqlMapper.RemoveTypeMap(typeof(Guid));
            SqlMapper.RemoveTypeMap(typeof(Guid?));
            SqlMapper.AddTypeHandler(new GuidConverter());
            SqlMapper.AddTypeHandler(new CommandConverter());
            SqlMapper.AddTypeHandler(new SystemVersionConverter());
        }

        private static void RegisterProviderSettingConverter()
        {
            var settingTypes = typeof(IProviderConfig).Assembly.ImplementationsOf<IProviderConfig>()
                .Where(x => !x.ContainsGenericParameters);

            var providerSettingConverter = new ProviderSettingConverter();
            foreach (var embeddedType in settingTypes)
            {
                SqlMapper.AddTypeHandler(embeddedType, providerSettingConverter);
            }
        }

        private static void RegisterEmbeddedConverter()
        {
            var embeddedTypes = typeof(IEmbeddedDocument).Assembly.ImplementationsOf<IEmbeddedDocument>();

            var embeddedConverterDefinition = typeof(EmbeddedDocumentConverter<>).GetGenericTypeDefinition();
            var genericListDefinition = typeof(List<>).GetGenericTypeDefinition();

            foreach (var embeddedType in embeddedTypes)
            {
                var embeddedListType = genericListDefinition.MakeGenericType(embeddedType);

                RegisterEmbeddedConverter(embeddedType, embeddedConverterDefinition);
                RegisterEmbeddedConverter(embeddedListType, embeddedConverterDefinition);
            }
        }

        private static void RegisterEmbeddedConverter(Type embeddedType, Type embeddedConverterDefinition)
        {
            var embeddedConverterType = embeddedConverterDefinition.MakeGenericType(embeddedType);
            var converter = (ITypeHandler)Activator.CreateInstance(embeddedConverterType);

            SqlMapper.AddTypeHandler(embeddedType, converter);
        }
    }
}
