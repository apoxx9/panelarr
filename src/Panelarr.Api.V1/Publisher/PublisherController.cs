using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;
using NzbDrone.Http.REST.Attributes;
using Panelarr.Http;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Publisher
{
    [V1ApiController]
    public class PublisherController : RestController<PublisherResource>
    {
        private readonly IPublisherService _publisherService;

        public PublisherController(IPublisherService publisherService)
        {
            _publisherService = publisherService;
        }

        protected override PublisherResource GetResourceById(int id)
        {
            return _publisherService.GetPublisher(id).ToResource();
        }

        [HttpGet]
        public List<PublisherResource> GetAllPublishers()
        {
            return _publisherService.GetAllPublishers().ToResource();
        }

        [RestPostById]
        public ActionResult<PublisherResource> CreatePublisher([FromBody] PublisherResource publisherResource)
        {
            var publisher = _publisherService.AddPublisher(publisherResource.ToModel());
            return Created(publisher.Id);
        }

        [RestPutById]
        public ActionResult<PublisherResource> UpdatePublisher([FromBody] PublisherResource publisherResource)
        {
            var publisher = _publisherService.UpdatePublisher(publisherResource.ToModel());
            return Accepted(publisher.ToResource());
        }

        [RestDeleteById]
        public void DeletePublisher(int id)
        {
            _publisherService.DeletePublisher(id);
        }
    }
}
