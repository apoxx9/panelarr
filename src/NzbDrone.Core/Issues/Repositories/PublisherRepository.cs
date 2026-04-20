using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public interface IPublisherRepository : IBasicRepository<Publisher>
    {
        Publisher FindByForeignId(string foreignPublisherId);
        List<Publisher> FindByForeignId(List<string> foreignPublisherIds);
    }

    public class PublisherRepository : BasicRepository<Publisher>, IPublisherRepository
    {
        public PublisherRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public Publisher FindByForeignId(string foreignPublisherId)
        {
            return Query(x => x.ForeignPublisherId == foreignPublisherId).SingleOrDefault();
        }

        public List<Publisher> FindByForeignId(List<string> foreignPublisherIds)
        {
            return Query(x => foreignPublisherIds.Contains(x.ForeignPublisherId));
        }
    }
}
