using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.ReadingLists
{
    public interface IReadingListRepository : IBasicRepository<ReadingList>
    {
        ReadingList FindByForeignReadingListId(string foreignReadingListId);
    }

    public class ReadingListRepository : BasicRepository<ReadingList>, IReadingListRepository
    {
        public ReadingListRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public ReadingList FindByForeignReadingListId(string foreignReadingListId)
        {
            return Query(a => a.ForeignReadingListId == foreignReadingListId).FirstOrDefault();
        }
    }

    public interface IReadingListItemRepository : IBasicRepository<ReadingListItem>
    {
        List<ReadingListItem> FindByReadingListId(int readingListId);
        List<ReadingListItem> FindByIssueIds(List<int> issueIds);
        void DeleteByReadingListId(int readingListId);
    }

    public class ReadingListItemRepository : BasicRepository<ReadingListItem>, IReadingListItemRepository
    {
        public ReadingListItemRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<ReadingListItem> FindByReadingListId(int readingListId)
        {
            return Query(s => s.ReadingListId == readingListId).OrderBy(s => s.Position).ToList();
        }

        public List<ReadingListItem> FindByIssueIds(List<int> issueIds)
        {
            return Query(s => s.IssueId.HasValue && Enumerable.Contains(issueIds, s.IssueId.Value));
        }

        public void DeleteByReadingListId(int readingListId)
        {
            Delete(s => s.ReadingListId == readingListId);
        }
    }
}
