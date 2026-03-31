using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Common;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFileRepository : IBasicRepository<ComicFile>
    {
        List<ComicFile> GetFilesBySeries(int seriesId);
        List<ComicFile> GetFilesBySeriesMetadataId(int seriesMetadataId);
        List<ComicFile> GetFilesByIssue(int issueId);
        List<ComicFile> GetUnmappedFiles();
        List<ComicFile> GetFilesWithBasePath(string path);
        List<ComicFile> GetFileWithPath(List<string> paths);
        ComicFile GetFileWithPath(string path);
        void DeleteFilesByIssue(int issueId);
        void UnlinkFilesByIssue(int issueId);
    }

    public class MediaFileRepository : BasicRepository<ComicFile>, IMediaFileRepository
    {
        public MediaFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        // always join with all the other good stuff
        protected override SqlBuilder Builder() => new SqlBuilder(_database.DatabaseType)
            .LeftJoin<ComicFile, Issue>((f, b) => f.IssueId == b.Id)
            .LeftJoin<Issue, Series>((issue, series) => issue.SeriesMetadataId == series.SeriesMetadataId)
            .LeftJoin<Series, SeriesMetadata>((a, m) => a.SeriesMetadataId == m.Id);

        protected override List<ComicFile> Query(SqlBuilder builder) => Query(_database, builder).ToList();

        public static IEnumerable<ComicFile> Query(IDatabase database, SqlBuilder builder)
        {
            return database.QueryJoined<ComicFile, Issue, Series, SeriesMetadata>(builder, (file, issue, series, metadata) => Map(file, issue, series, metadata));
        }

        private static ComicFile Map(ComicFile file, Issue issue, Series series, SeriesMetadata metadata)
        {
            file.Issue = issue;

            if (series != null)
            {
                series.Metadata = metadata;
            }

            file.Series = series;

            return file;
        }

        public List<ComicFile> GetFilesBySeries(int seriesId)
        {
            return Query(Builder().Where<Series>(a => a.Id == seriesId));
        }

        public List<ComicFile> GetFilesBySeriesMetadataId(int seriesMetadataId)
        {
            return Query(Builder().Where<Issue>(b => b.SeriesMetadataId == seriesMetadataId));
        }

        public List<ComicFile> GetFilesByIssue(int issueId)
        {
            return Query(Builder().Where<Issue>(b => b.Id == issueId));
        }

        public List<ComicFile> GetUnmappedFiles()
        {
            return _database.Query<ComicFile>(new SqlBuilder(_database.DatabaseType).Select(typeof(ComicFile))
                                              .Where<ComicFile>(t => t.IssueId == 0)).ToList();
        }

        public void DeleteFilesByIssue(int issueId)
        {
            var fileIds = GetFilesByIssue(issueId).Select(x => x.Id).ToList();
            Delete(x => fileIds.Contains(x.Id));
        }

        public void UnlinkFilesByIssue(int issueId)
        {
            var files = GetFilesByIssue(issueId);
            files.ForEach(x => x.IssueId = 0);
            SetFields(files, f => f.IssueId);
        }

        public List<ComicFile> GetFilesWithBasePath(string path)
        {
            // ensure path ends with a single trailing path separator to avoid matching partial paths
            var safePath = path.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return _database.Query<ComicFile>(new SqlBuilder(_database.DatabaseType).Where<ComicFile>(x => x.Path.StartsWith(safePath))).ToList();
        }

        public ComicFile GetFileWithPath(string path)
        {
            return Query(x => x.Path == path).SingleOrDefault();
        }

        public List<ComicFile> GetFileWithPath(List<string> paths)
        {
            // use more limited join for speed
            var builder = new SqlBuilder(_database.DatabaseType)
                .LeftJoin<ComicFile, Issue>((f, t) => f.IssueId == t.Id);

            var all = _database.QueryJoined<ComicFile, Issue>(builder, (file, issue) => MapTrack(file, issue)).ToList();

            var joined = all.Join(paths, x => x.Path, x => x, (file, path) => file, PathEqualityComparer.Instance).ToList();
            return joined;
        }

        private ComicFile MapTrack(ComicFile file, Issue issue)
        {
            file.Issue = issue;
            return file;
        }
    }
}
