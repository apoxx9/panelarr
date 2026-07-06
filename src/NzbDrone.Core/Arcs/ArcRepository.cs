using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Arcs
{
    public interface IArcRepository : IBasicRepository<Arc>
    {
        Arc FindByForeignArcId(string foreignArcId);
    }

    public class ArcRepository : BasicRepository<Arc>, IArcRepository
    {
        public ArcRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public Arc FindByForeignArcId(string foreignArcId)
        {
            return Query(a => a.ForeignArcId == foreignArcId).FirstOrDefault();
        }
    }

    public interface IArcIssueRepository : IBasicRepository<ArcIssue>
    {
        List<ArcIssue> FindByArcId(int arcId);
        List<ArcIssue> FindByIssueIds(List<int> issueIds);
        void DeleteByArcId(int arcId);
    }

    public class ArcIssueRepository : BasicRepository<ArcIssue>, IArcIssueRepository
    {
        public ArcIssueRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<ArcIssue> FindByArcId(int arcId)
        {
            return Query(s => s.ArcId == arcId).OrderBy(s => s.Position).ToList();
        }

        public List<ArcIssue> FindByIssueIds(List<int> issueIds)
        {
            return Query(s => s.IssueId.HasValue && Enumerable.Contains(issueIds, s.IssueId.Value));
        }

        public void DeleteByArcId(int arcId)
        {
            Delete(s => s.ArcId == arcId);
        }
    }
}
