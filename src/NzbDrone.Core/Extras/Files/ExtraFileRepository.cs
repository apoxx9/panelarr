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
        void DeleteForBook(int authorId, int bookId);
        void DeleteForBookFile(int bookFileId);
        List<TExtraFile> GetFilesBySeries(int authorId);
        List<TExtraFile> GetFilesByBook(int authorId, int bookId);
        List<TExtraFile> GetFilesByBookFile(int bookFileId);
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

        public void DeleteForBook(int authorId, int bookId)
        {
            Delete(c => c.SeriesId == authorId && c.IssueId == bookId);
        }

        public void DeleteForBookFile(int bookFileId)
        {
            Delete(c => c.ComicFileId == bookFileId);
        }

        public List<TExtraFile> GetFilesBySeries(int authorId)
        {
            return Query(c => c.SeriesId == authorId);
        }

        public List<TExtraFile> GetFilesByBook(int authorId, int bookId)
        {
            return Query(c => c.SeriesId == authorId && c.IssueId == bookId);
        }

        public List<TExtraFile> GetFilesByBookFile(int bookFileId)
        {
            return Query(c => c.ComicFileId == bookFileId);
        }

        public TExtraFile FindByPath(int authorId, string path)
        {
            return Query(c => c.SeriesId == authorId && c.RelativePath == path).SingleOrDefault();
        }
    }
}
