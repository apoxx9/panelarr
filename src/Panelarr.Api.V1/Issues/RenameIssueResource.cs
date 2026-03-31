using System.Collections.Generic;
using System.Linq;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Issues
{
    public class RenameIssueResource : RestResource
    {
        public int SeriesId { get; set; }
        public int IssueId { get; set; }
        public int ComicFileId { get; set; }
        public string ExistingPath { get; set; }
        public string NewPath { get; set; }
    }

    public static class RenameIssueResourceMapper
    {
        public static RenameIssueResource ToResource(this NzbDrone.Core.MediaFiles.RenameComicFilePreview model)
        {
            if (model == null)
            {
                return null;
            }

            return new RenameIssueResource
            {
                SeriesId = model.SeriesId,
                IssueId = model.IssueId,
                ComicFileId = model.ComicFileId,
                ExistingPath = model.ExistingPath,
                NewPath = model.NewPath
            };
        }

        public static List<RenameIssueResource> ToResource(this IEnumerable<NzbDrone.Core.MediaFiles.RenameComicFilePreview> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
