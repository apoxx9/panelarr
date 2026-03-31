using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Issues;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.SeriesGroup
{
    public class SeriesIssueLinkResource : RestResource
    {
        public string Position { get; set; }
        public int SeriesPosition { get; set; }
        public int SeriesId { get; set; }
        public int IssueId { get; set; }
    }

    public static class SeriesIssueLinkResourceMapper
    {
        public static SeriesIssueLinkResource ToResource(this SeriesGroupLink model)
        {
            return new SeriesIssueLinkResource
            {
                Id = model.Id,
                Position = model.Position,
                SeriesPosition = model.SeriesPosition,
                SeriesId = model.SeriesGroupId,
                IssueId = model.SeriesMetadataId
            };
        }

        public static List<SeriesIssueLinkResource> ToResource(this IEnumerable<SeriesGroupLink> models)
        {
            return models?.Select(ToResource).ToList();
        }
    }
}
