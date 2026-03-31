using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Extras.Files
{
    public interface IExtraFileRepository<TExtraFile> : IBasicRepository<TExtraFile>
        where TExtraFile : ExtraFile, new()
    {
        void DeleteForSeries(int authorId);
        void DeleteForIssue(int authorId, int issueId);
        void DeleteForComicFile(int comicFileId);
        List<TExtraFile> GetFilesBySeries(int authorId);
        List<TExtraFile> GetFilesByIssue(int authorId, int issueId);
        List<TExtraFile> GetFilesByComicFile(int comicFileId);
        TExtraFile FindByPath(int authorId, string path);
    }

    public class ExtraFileRepository<TExtraFile> : BasicRepository<TExtraFile>, IExtraFileRepository<TExtraFile>
        where TExtraFile : ExtraFile, new()
    {
        public ExtraFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public void DeleteForSeries(int authorId)
        {
            Delete(c => c.SeriesId == authorId);
        }

        public void DeleteForIssue(int authorId, int issueId)
        {
            Delete(c => c.SeriesId == authorId && c.IssueId == issueId);
        }

        public void DeleteForComicFile(int comicFileId)
        {
            Delete(c => c.ComicFileId == comicFileId);
        }

        public List<TExtraFile> GetFilesBySeries(int authorId)
        {
            return Query(c => c.SeriesId == authorId);
        }

        public List<TExtraFile> GetFilesByIssue(int authorId, int issueId)
        {
            return Query(c => c.SeriesId == authorId && c.IssueId == issueId);
        }

        public List<TExtraFile> GetFilesByComicFile(int comicFileId)
        {
            return Query(c => c.ComicFileId == comicFileId);
        }

        public TExtraFile FindByPath(int authorId, string path)
        {
            return Query(c => c.SeriesId == authorId && c.RelativePath == path).SingleOrDefault();
        }
    }
}
