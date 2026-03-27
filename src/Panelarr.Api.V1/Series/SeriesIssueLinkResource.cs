using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Books;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.SeriesGroup
{
    public class SeriesBookLinkResource : RestResource
    {
        public string Position { get; set; }
        public int SeriesPosition { get; set; }
        public int SeriesId { get; set; }
        public int IssueId { get; set; }
    }

    public static class SeriesBookLinkResourceMapper
    {
        public static SeriesBookLinkResource ToResource(this SeriesGroupLink model)
        {
            return new SeriesBookLinkResource
            {
                Id = model.Id,
                Position = model.Position,
                SeriesPosition = model.SeriesPosition,
                SeriesId = model.SeriesId,
                IssueId = model.IssueId
            };
        }

        public static List<SeriesBookLinkResource> ToResource(this IEnumerable<SeriesGroupLink> models)
        {
            return models?.Select(ToResource).ToList();
        }
    }
}
