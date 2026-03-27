using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Books
{
    public interface IBookRepository : IBasicRepository<Issue>
    {
        List<Issue> GetBooks(int authorId);
        List<Issue> GetLastBooks(IEnumerable<int> authorMetadataIds);
        List<Issue> GetNextBooks(IEnumerable<int> authorMetadataIds);
        List<Issue> GetBooksBySeriesMetadataId(int authorMetadataId);
        List<Issue> GetBooksForRefresh(int authorMetadataId, List<string> foreignIds);
        List<Issue> GetBooksByFileIds(IEnumerable<int> fileIds);
        Issue FindByTitle(int authorMetadataId, string title);
        Issue FindById(string foreignBookId);
        Issue FindBySlug(string titleSlug);
        PagingSpec<Issue> IssuesWithoutFiles(PagingSpec<Issue> pagingSpec);
        PagingSpec<Issue> IssuesWhereCutoffUnmet(PagingSpec<Issue> pagingSpec, List<QualitiesBelowCutoff> qualitiesBelowCutoff);
        List<Issue> IssuesBetweenDates(DateTime startDate, DateTime endDate, bool includeUnmonitored);
        List<Issue> SeriesBooksBetweenDates(Series author, DateTime startDate, DateTime endDate, bool includeUnmonitored);
        void SetMonitoredFlat(Issue issue, bool monitored);
        void SetMonitored(IEnumerable<int> ids, bool monitored);
        List<Issue> GetSeriesBooksWithFiles(Series author);
    }

    public class IssueRepository : BasicRepository<Issue>, IBookRepository
    {
        public IssueRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Issue> GetBooks(int authorId)
        {
            return Query(Builder().Join<Issue, Series>((l, r) => l.SeriesMetadataId == r.SeriesMetadataId).Where<Series>(a => a.Id == authorId));
        }

        public List<Issue> GetLastBooks(IEnumerable<int> authorMetadataIds)
        {
            var now = DateTime.UtcNow;

            var inner = Builder()
                .Select("MIN(\"Issues\".\"Id\") as id, MAX(\"Issues\".\"ReleaseDate\") as date")
                .Where<Issue>(x => authorMetadataIds.Contains(x.SeriesMetadataId) && x.ReleaseDate < now)
                .GroupBy<Issue>(x => x.SeriesMetadataId)
                .AddSelectTemplate(typeof(Issue));

            var outer = Builder()
                .Join($"({inner.RawSql}) ids on ids.id = \"Issues\".\"Id\" and ids.date = \"Issues\".\"ReleaseDate\"")
                .AddParameters(inner.Parameters);

            return Query(outer);
        }

        public List<Issue> GetNextBooks(IEnumerable<int> authorMetadataIds)
        {
            var now = DateTime.UtcNow;

            var inner = Builder()
                .Select("MIN(\"Issues\".\"Id\") as id, MIN(\"Issues\".\"ReleaseDate\") as date")
                .Where<Issue>(x => authorMetadataIds.Contains(x.SeriesMetadataId) && x.ReleaseDate > now)
                .GroupBy<Issue>(x => x.SeriesMetadataId)
                .AddSelectTemplate(typeof(Issue));

            var outer = Builder()
                .Join($"({inner.RawSql}) ids on ids.id = \"Issues\".\"Id\" and ids.date = \"Issues\".\"ReleaseDate\"")
                .AddParameters(inner.Parameters);

            return Query(outer);
        }

        public List<Issue> GetBooksBySeriesMetadataId(int authorMetadataId)
        {
            return Query(s => s.SeriesMetadataId == authorMetadataId);
        }

        public List<Issue> GetBooksForRefresh(int authorMetadataId, List<string> foreignIds)
        {
            return Query(a => a.SeriesMetadataId == authorMetadataId || foreignIds.Contains(a.ForeignIssueId));
        }

        public List<Issue> GetBooksByFileIds(IEnumerable<int> fileIds)
        {
            return Query(new SqlBuilder(_database.DatabaseType)
                         .Join<Issue, ComicFile>((b, f) => b.Id == f.IssueId)
                         .Where<ComicFile>(f => fileIds.Contains(f.Id)))
                .DistinctBy(x => x.Id)
                .ToList();
        }

        public Issue FindById(string foreignBookId)
        {
            return Query(s => s.ForeignIssueId == foreignBookId).SingleOrDefault();
        }

        public Issue FindBySlug(string titleSlug)
        {
            return Query(s => s.TitleSlug == titleSlug).SingleOrDefault();
        }

        //x.Id == null is converted to SQL, so warning incorrect
#pragma warning disable CS0472
        private SqlBuilder IssuesWithoutFilesBuilder(DateTime currentTime) => Builder()
            .Join<Issue, Series>((l, r) => l.SeriesMetadataId == r.SeriesMetadataId)
            .Join<Series, SeriesMetadata>((l, r) => l.SeriesMetadataId == r.Id)
            .LeftJoin<Issue, ComicFile>((b, f) => b.Id == f.IssueId)
            .Where<ComicFile>(f => f.Id == null)
            .Where<Issue>(a => a.Monitored == true)
            .Where<Issue>(a => a.ReleaseDate <= currentTime);
#pragma warning restore CS0472

        public PagingSpec<Issue> IssuesWithoutFiles(PagingSpec<Issue> pagingSpec)
        {
            var currentTime = DateTime.UtcNow;

            pagingSpec.Records = GetPagedRecords(IssuesWithoutFilesBuilder(currentTime), pagingSpec, PagedQuery);
            pagingSpec.TotalRecords = GetPagedRecordCount(IssuesWithoutFilesBuilder(currentTime).SelectCountDistinct<Issue>(x => x.Id), pagingSpec);

            return pagingSpec;
        }

        private SqlBuilder IssuesWhereCutoffUnmetBuilder(List<QualitiesBelowCutoff> qualitiesBelowCutoff) => Builder()
            .Join<Issue, Series>((l, r) => l.SeriesMetadataId == r.SeriesMetadataId)
            .Join<Series, SeriesMetadata>((l, r) => l.SeriesMetadataId == r.Id)
            .LeftJoin<Issue, ComicFile>((b, f) => b.Id == f.IssueId)
            .Where<Issue>(e => e.Monitored == true)
            .Where(BuildQualityCutoffWhereClause(qualitiesBelowCutoff));

        private string BuildQualityCutoffWhereClause(List<QualitiesBelowCutoff> qualitiesBelowCutoff)
        {
            var clauses = new List<string>();

            foreach (var profile in qualitiesBelowCutoff)
            {
                foreach (var belowCutoff in profile.QualityIds)
                {
                    clauses.Add(string.Format("(\"Series\".\"QualityProfileId\" = {0} AND \"ComicFiles\".\"Quality\" LIKE '%_quality_: {1},%')", profile.ProfileId, belowCutoff));
                }
            }

            return string.Format("({0})", string.Join(" OR ", clauses));
        }

        public PagingSpec<Issue> IssuesWhereCutoffUnmet(PagingSpec<Issue> pagingSpec, List<QualitiesBelowCutoff> qualitiesBelowCutoff)
        {
            pagingSpec.Records = GetPagedRecords(IssuesWhereCutoffUnmetBuilder(qualitiesBelowCutoff), pagingSpec, PagedQuery);

            var countTemplate = $"SELECT COUNT(*) FROM (SELECT /**select**/ FROM \"{TableMapping.Mapper.TableNameMapping(typeof(Issue))}\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/) AS \"Inner\"";
            pagingSpec.TotalRecords = GetPagedRecordCount(IssuesWhereCutoffUnmetBuilder(qualitiesBelowCutoff).Select(typeof(Issue)), pagingSpec, countTemplate);

            return pagingSpec;
        }

        public List<Issue> IssuesBetweenDates(DateTime startDate, DateTime endDate, bool includeUnmonitored)
        {
            var builder = Builder().Where<Issue>(rg => rg.ReleaseDate >= startDate && rg.ReleaseDate <= endDate);

            if (!includeUnmonitored)
            {
                builder = builder.Where<Issue>(e => e.Monitored == true)
                    .Join<Issue, Series>((l, r) => l.SeriesMetadataId == r.SeriesMetadataId)
                    .Where<Series>(e => e.Monitored == true);
            }

            return Query(builder);
        }

        public List<Issue> SeriesBooksBetweenDates(Series author, DateTime startDate, DateTime endDate, bool includeUnmonitored)
        {
            var builder = Builder().Where<Issue>(rg => rg.ReleaseDate >= startDate &&
                                                 rg.ReleaseDate <= endDate &&
                                                 rg.SeriesMetadataId == author.SeriesMetadataId);

            if (!includeUnmonitored)
            {
                builder = builder.Where<Issue>(e => e.Monitored == true)
                    .Join<Issue, Series>((l, r) => l.SeriesMetadataId == r.SeriesMetadataId)
                    .Where<Series>(e => e.Monitored == true);
            }

            return Query(builder);
        }

        public void SetMonitoredFlat(Issue issue, bool monitored)
        {
            issue.Monitored = monitored;
            SetFields(issue, p => p.Monitored);

            ModelUpdated(issue, true);
        }

        public void SetMonitored(IEnumerable<int> ids, bool monitored)
        {
            var issues = ids.Select(x => new Issue { Id = x, Monitored = monitored }).ToList();
            SetFields(issues, p => p.Monitored);
        }

        public Issue FindByTitle(int authorMetadataId, string title)
        {
            var cleanTitle = Parser.Parser.CleanSeriesName(title);

            if (string.IsNullOrEmpty(cleanTitle))
            {
                cleanTitle = title;
            }

            return Query(s => (s.CleanTitle == cleanTitle || s.Title == title) && s.SeriesMetadataId == authorMetadataId)
                .ExclusiveOrDefault();
        }

        public List<Issue> GetSeriesBooksWithFiles(Series author)
        {
            return Query(Builder()
                         .Join<Issue, ComicFile>((b, f) => b.Id == f.IssueId)
                         .Where<Issue>(x => x.SeriesMetadataId == author.SeriesMetadataId)
                         .Where<Issue>(e => e.Monitored == true));
        }
    }
}
