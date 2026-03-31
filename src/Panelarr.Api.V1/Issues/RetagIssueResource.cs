using System.Collections.Generic;
using System.Linq;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Issues
{
    public class TagDifference
    {
        public string Field { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }

    public class RetagIssueResource : RestResource
    {
        public int SeriesId { get; set; }
        public int IssueId { get; set; }
        public List<int> TrackNumbers { get; set; }
        public int ComicFileId { get; set; }
        public string Path { get; set; }
        public List<TagDifference> Changes { get; set; }
    }

    public static class RetagTrackResourceMapper
    {
        public static RetagIssueResource ToResource(this NzbDrone.Core.MediaFiles.RetagComicFilePreview model)
        {
            if (model == null)
            {
                return null;
            }

            return new RetagIssueResource
            {
                SeriesId = model.SeriesId,
                IssueId = model.IssueId,
                TrackNumbers = model.TrackNumbers.ToList(),
                ComicFileId = model.ComicFileId,
                Path = model.Path,
                Changes = model.Changes.Select(x => new TagDifference
                {
                    Field = x.Key,
                    OldValue = x.Value.Item1,
                    NewValue = x.Value.Item2
                }).ToList()
            };
        }

        public static List<RetagIssueResource> ToResource(this IEnumerable<NzbDrone.Core.MediaFiles.RetagComicFilePreview> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
