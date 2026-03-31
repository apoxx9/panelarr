using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.Issues
{
    public interface IPublisherService
    {
        Publisher GetPublisher(int id);
        List<Publisher> GetAllPublishers();
        Publisher FindByForeignId(string foreignPublisherId);
        Publisher AddPublisher(Publisher publisher);
        Publisher UpdatePublisher(Publisher publisher);
        void DeletePublisher(int id);
    }

    public class PublisherService : IPublisherService
    {
        private readonly IPublisherRepository _publisherRepository;

        public PublisherService(IPublisherRepository publisherRepository)
        {
            _publisherRepository = publisherRepository;
        }

        public Publisher GetPublisher(int id)
        {
            return _publisherRepository.Get(id);
        }

        public List<Publisher> GetAllPublishers()
        {
            return _publisherRepository.All().ToList();
        }

        public Publisher FindByForeignId(string foreignPublisherId)
        {
            return _publisherRepository.FindByForeignId(foreignPublisherId);
        }

        public Publisher AddPublisher(Publisher publisher)
        {
            return _publisherRepository.Insert(publisher);
        }

        public Publisher UpdatePublisher(Publisher publisher)
        {
            return _publisherRepository.Update(publisher);
        }

        public void DeletePublisher(int id)
        {
            _publisherRepository.Delete(id);
        }
    }
}
