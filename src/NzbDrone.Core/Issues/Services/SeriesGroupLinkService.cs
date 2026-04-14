using System.Collections.Generic;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface ISeriesIssueLinkService
    {
        List<SeriesGroupLink> GetLinksBySeries(int seriesId);
        List<SeriesGroupLink> GetLinksBySeriesAndSeries(int seriesId, string foreignSeriesId);
        List<SeriesGroupLink> GetLinksByIssue(List<int> issueIds);
        void InsertMany(List<SeriesGroupLink> model);
        void UpdateMany(List<SeriesGroupLink> model);
        void DeleteMany(List<SeriesGroupLink> model);
    }

    public class SeriesGroupLinkService : ISeriesIssueLinkService,
        IHandle<IssueDeletedEvent>
    {
        private readonly ISeriesIssueLinkRepository _repo;

        public SeriesGroupLinkService(ISeriesIssueLinkRepository repo)
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

        public List<SeriesGroupLink> GetLinksByIssue(List<int> issueIds)
        {
            return _repo.GetLinksByIssue(issueIds);
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
            var links = GetLinksByIssue(new List<int> { message.Issue.Id });
            DeleteMany(links);
        }
    }
}
