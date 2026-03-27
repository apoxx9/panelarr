using System.Collections.Generic;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Books
{
    public interface ISeriesBookLinkService
    {
        List<SeriesGroupLink> GetLinksBySeries(int seriesId);
        List<SeriesGroupLink> GetLinksBySeriesAndSeries(int seriesId, string foreignSeriesId);
        List<SeriesGroupLink> GetLinksByBook(List<int> bookIds);
        void InsertMany(List<SeriesGroupLink> model);
        void UpdateMany(List<SeriesGroupLink> model);
        void DeleteMany(List<SeriesGroupLink> model);
    }

    public class SeriesGroupLinkService : ISeriesBookLinkService,
        IHandle<IssueDeletedEvent>
    {
        private readonly ISeriesBookLinkRepository _repo;

        public SeriesGroupLinkService(ISeriesBookLinkRepository repo)
        {
            _repo = repo;
        }

        public List<SeriesGroupLink> GetLinksBySeries(int seriesId)
        {
            return _repo.GetLinksBySeries(seriesId);
        }

        public List<SeriesGroupLink> GetLinksBySeriesAndSeries(int seriesId, string foreignSeriesId)
        {
            return _repo.GetLinksBySeriesAndSeries(seriesId, foreignSeriesId);
        }

        public List<SeriesGroupLink> GetLinksByBook(List<int> bookIds)
        {
            return _repo.GetLinksByBook(bookIds);
        }

        public void InsertMany(List<SeriesGroupLink> model)
        {
            _repo.InsertMany(model);
        }

        public void UpdateMany(List<SeriesGroupLink> model)
        {
            _repo.UpdateMany(model);
        }

        public void DeleteMany(List<SeriesGroupLink> model)
        {
            _repo.DeleteMany(model);
        }

        public void Handle(IssueDeletedEvent message)
        {
            var links = GetLinksByBook(new List<int> { message.Issue.Id });
            DeleteMany(links);
        }
    }
}
