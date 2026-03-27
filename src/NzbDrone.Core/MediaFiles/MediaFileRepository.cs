using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Common;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMediaFileRepository : IBasicRepository<ComicFile>
    {
        List<ComicFile> GetFilesBySeries(int authorId);
        List<ComicFile> GetFilesBySeriesMetadataId(int authorMetadataId);
        List<ComicFile> GetFilesByBook(int bookId);
        List<ComicFile> GetFilesByEdition(int editionId);
        List<ComicFile> GetUnmappedFiles();
        List<ComicFile> GetFilesWithBasePath(string path);
        List<ComicFile> GetFileWithPath(List<string> paths);
        ComicFile GetFileWithPath(string path);
        void DeleteFilesByBook(int bookId);
        void UnlinkFilesByBook(int bookId);
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
            .LeftJoin<Issue, Series>((issue, author) => issue.SeriesMetadataId == author.SeriesMetadataId)
            .LeftJoin<Series, SeriesMetadata>((a, m) => a.SeriesMetadataId == m.Id);

        protected override List<ComicFile> Query(SqlBuilder builder) => Query(_database, builder).ToList();

        public static IEnumerable<ComicFile> Query(IDatabase database, SqlBuilder builder)
        {
            return database.QueryJoined<ComicFile, Issue, Series, SeriesMetadata>(builder, (file, issue, author, metadata) => Map(file, issue, author, metadata));
        }

        private static ComicFile Map(ComicFile file, Issue issue, Series author, SeriesMetadata metadata)
        {
            file.Issue = issue;

            if (author != null)
            {
                author.Metadata = metadata;
            }

            file.Series = author;

            return file;
        }

        public List<ComicFile> GetFilesBySeries(int authorId)
        {
            return Query(Builder().Where<Series>(a => a.Id == authorId));
        }

        public List<ComicFile> GetFilesBySeriesMetadataId(int authorMetadataId)
        {
            return Query(Builder().Where<Issue>(b => b.SeriesMetadataId == authorMetadataId));
        }

        public List<ComicFile> GetFilesByBook(int bookId)
        {
            return Query(Builder().Where<Issue>(b => b.Id == bookId));
        }

        public List<ComicFile> GetFilesByEdition(int editionId)
        {
            // Edition is gone - treat editionId as IssueId for backwards compatibility
            return Query(Builder().Where<ComicFile>(f => f.IssueId == editionId));
        }

        public List<ComicFile> GetUnmappedFiles()
        {
            return _database.Query<ComicFile>(new SqlBuilder(_database.DatabaseType).Select(typeof(ComicFile))
                                              .Where<ComicFile>(t => t.IssueId == 0)).ToList();
        }

        public void DeleteFilesByBook(int bookId)
        {
            var fileIds = GetFilesByBook(bookId).Select(x => x.Id).ToList();
            Delete(x => fileIds.Contains(x.Id));
        }

        public void UnlinkFilesByBook(int bookId)
        {
            var files = GetFilesByBook(bookId);
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
