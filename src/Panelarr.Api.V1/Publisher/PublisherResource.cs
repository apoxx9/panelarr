using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.MediaCover;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Publisher
{
    public class PublisherResource : RestResource
    {
        public string ForeignPublisherId { get; set; }
        public string Name { get; set; }
        public string CleanName { get; set; }
        public string Description { get; set; }
        public List<MediaCover> Images { get; set; }
    }

    public static class PublisherResourceMapper
    {
        public static PublisherResource ToResource(this NzbDrone.Core.Issues.Publisher model)
        {
            if (model == null)
            {
                return null;
            }

            return new PublisherResource
            {
                Id = model.Id,
                ForeignPublisherId = model.ForeignPublisherId,
                Name = model.Name,
                CleanName = model.CleanName,
                Description = model.Description,
                Images = model.Images
            };
        }

        public static NzbDrone.Core.Issues.Publisher ToModel(this PublisherResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new NzbDrone.Core.Issues.Publisher
            {
                Id = resource.Id,
                ForeignPublisherId = resource.ForeignPublisherId,
                Name = resource.Name,
                CleanName = resource.CleanName,
                Description = resource.Description,
                Images = resource.Images ?? new List<MediaCover>()
            };
        }

        public static List<PublisherResource> ToResource(this IEnumerable<NzbDrone.Core.Issues.Publisher> models)
        {
            return models?.Select(ToResource).ToList();
        }
    }
}
