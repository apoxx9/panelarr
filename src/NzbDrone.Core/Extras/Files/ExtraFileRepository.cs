using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Extras.Files
{
    public interface IExtraFileRepository<TExtraFile> : IBasicRepository<TExtraFile>
        where TExtraFile : ExtraFile, new()
    {
        void DeleteForSeries(int seriesId);
        void DeleteForIssue(int seriesId, int issueId);
        void DeleteForComicFile(int comicFileId);
        List<TExtraFile> GetFilesBySeries(int seriesId);
        List<TExtraFile> GetFilesByIssue(int seriesId, int issueId);
        List<TExtraFile> GetFilesByComicFile(int comicFileId);
        TExtraFile FindByPath(int seriesId, string path);
    }

    public class ExtraFileRepository<TExtraFile> : BasicRepository<TExtraFile>, IExtraFileRepository<TExtraFile>
        where TExtraFile : ExtraFile, new()
    {
        public ExtraFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public void DeleteForSeries(int seriesId)
        {
            Delete(c => c.SeriesId == seriesId);
        }

        public void DeleteForIssue(int seriesId, int issueId)
        {
            Delete(c => c.SeriesId == seriesId && c.IssueId == issueId);
        }

        public void DeleteForComicFile(int comicFileId)
        {
            Delete(c => c.ComicFileId == comicFileId);
        }

        public List<TExtraFile> GetFilesBySeries(int seriesId)
        {
            return Query(c => c.SeriesId == seriesId);
        }

        public List<TExtraFile> GetFilesByIssue(int seriesId, int issueId)
        {
            return Query(c => c.SeriesId == seriesId && c.IssueId == issueId);
        }

        public List<TExtraFile> GetFilesByComicFile(int comicFileId)
        {
            return Query(c => c.ComicFileId == comicFileId);
        }

        public TExtraFile FindByPath(int seriesId, string path)
        {
            return Query(c => c.SeriesId == seriesId && c.RelativePath == path).SingleOrDefault();
        }
    }
}
