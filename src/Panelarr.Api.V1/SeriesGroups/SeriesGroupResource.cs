using System.Collections.Generic;
using System.Linq;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.SeriesGroups
{
    public class SeriesGroupResource : RestResource
    {
        public string ForeignSeriesGroupId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SortTitle { get; set; }
    }

    public static class SeriesGroupResourceMapper
    {
        public static SeriesGroupResource ToResource(this NzbDrone.Core.Issues.SeriesGroup model)
        {
            if (model == null)
            {
                return null;
            }

            return new SeriesGroupResource
            {
                Id = model.Id,
                ForeignSeriesGroupId = model.ForeignSeriesGroupId,
                Title = model.Title,
                Description = model.Description,
                SortTitle = model.SortTitle
            };
        }

        public static NzbDrone.Core.Issues.SeriesGroup ToModel(this SeriesGroupResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new NzbDrone.Core.Issues.SeriesGroup
            {
                Id = resource.Id,
                ForeignSeriesGroupId = resource.ForeignSeriesGroupId,
                Title = resource.Title,
                Description = resource.Description,
                SortTitle = resource.SortTitle
            };
        }

        public static List<SeriesGroupResource> ToResource(this IEnumerable<NzbDrone.Core.Issues.SeriesGroup> models)
        {
            return models?.Select(ToResource).ToList();
        }
    }
}
