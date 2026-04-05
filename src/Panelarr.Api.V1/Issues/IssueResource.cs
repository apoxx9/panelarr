using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaCover;
using Panelarr.Api.V1.Series;
using Panelarr.Http.REST;
using Swashbuckle.AspNetCore.Annotations;

namespace Panelarr.Api.V1.Issues
{
    public class IssueResource : RestResource
    {
        public string Title { get; set; }
        public string SeriesTitle { get; set; }
        public string Disambiguation { get; set; }
        public float? IssueNumber { get; set; }
        public string IssueType { get; set; }
        public string CoverArtUrl { get; set; }
        public int SeriesId { get; set; }
        public string ForeignIssueId { get; set; }
        public string TitleSlug { get; set; }
        public bool Monitored { get; set; }
        public Ratings Ratings { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public int PageCount { get; set; }
        public string Overview { get; set; }
        public List<string> Genres { get; set; }
        public List<Credit> Credits { get; set; }
        public SeriesResource Series { get; set; }
        public List<MediaCover> Images { get; set; }
        public List<Links> Links { get; set; }
        public IssueStatisticsResource Statistics { get; set; }
        public DateTime? Added { get; set; }
        public AddIssueOptions AddOptions { get; set; }
        public string RemoteCover { get; set; }
        public DateTime? LastSearchTime { get; set; }

        //Hiding this so people don't think its usable (only used to set the initial state)
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        [SwaggerIgnore]
        public bool Grabbed { get; set; }
    }

    public static class IssueResourceMapper
    {
        public static IssueResource ToResource(this Issue model)
        {
            if (model == null)
            {
                return null;
            }

            var seriesLinks = model.SeriesLinks?.Value?.OrderBy(x => x.SeriesPosition);
            var seriesTitle = seriesLinks?.Select(x => x?.SeriesGroup?.Value?.Title + (x?.Position.IsNotNullOrWhiteSpace() ?? false ? $" #{x.Position}" : string.Empty)).ConcatToString("; ");

            return new IssueResource
            {
                Id = model.Id,
                SeriesId = model.SeriesId,
                ForeignIssueId = model.ForeignIssueId,
                TitleSlug = model.TitleSlug,
                IssueNumber = model.IssueNumber > 0 ? model.IssueNumber : (float?)null,
                IssueType = model.IssueType != NzbDrone.Core.Issues.IssueType.Standard ? model.IssueType.ToString() : null,
                CoverArtUrl = model.CoverArtUrl,
                Monitored = model.Monitored,
                ReleaseDate = model.ReleaseDate,
                PageCount = model.PageCount,
                Overview = model.Overview,
                Genres = model.Genres,
                Credits = model.Credits ?? new List<Credit>(),
                Title = model.Title,
                SeriesTitle = seriesTitle,
                Images = model.CoverArtUrl.IsNotNullOrWhiteSpace()
                    ? new List<MediaCover>
                    {
                        new MediaCover
                        {
                            Url = model.CoverArtUrl,
                            CoverType = MediaCoverTypes.Cover
                        }
                    }
                    : new List<MediaCover>(),
                Links = model.Links ?? new List<Links>(),
                Ratings = model.Ratings ?? new Ratings(),
                Added = model.Added,
                LastSearchTime = model.LastSearchTime
            };
        }

        public static Issue ToModel(this IssueResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            var series = resource.Series?.ToModel() ?? new NzbDrone.Core.Issues.Series();

            return new Issue
            {
                Id = resource.Id,
                ForeignIssueId = resource.ForeignIssueId,
                TitleSlug = resource.TitleSlug,
                Title = resource.Title,
                Monitored = resource.Monitored,
                AddOptions = resource.AddOptions,
                Series = series,
                SeriesMetadata = series.Metadata.Value
            };
        }

        public static Issue ToModel(this IssueResource resource, Issue issue)
        {
            var updatedIssue = resource.ToModel();

            issue.ApplyChanges(updatedIssue);

            return issue;
        }

        public static List<IssueResource> ToResource(this IEnumerable<Issue> models)
        {
            return models?.Select(ToResource).ToList();
        }

        public static List<Issue> ToModel(this IEnumerable<IssueResource> resources)
        {
            return resources.Select(ToModel).ToList();
        }
    }
}
