using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.SeriesStats;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.Http.REST.Attributes;
using NzbDrone.SignalR;
using Panelarr.Http;

namespace Panelarr.Api.V1.Issues
{
    [V1ApiController]
    public class IssueController : IssueControllerWithSignalR,
        IHandle<IssueGrabbedEvent>,
        IHandle<IssueEditedEvent>,
        IHandle<IssueUpdatedEvent>,
        IHandle<IssueDeletedEvent>,
        IHandle<IssueImportedEvent>,
        IHandle<TrackImportedEvent>,
        IHandle<ComicFileDeletedEvent>
    {
        protected readonly ISeriesService _seriesService;
        protected readonly IAddIssueService _addIssueService;

        public IssueController(ISeriesService seriesService,
                          IIssueService issueService,
                          IAddIssueService addIssueService,
                          ISeriesIssueLinkService seriesIssueLinkService,
                          ISeriesStatisticsService seriesStatisticsService,
                          IMapCoversToLocal coverMapper,
                          IUpgradableSpecification upgradableSpecification,
                          IBroadcastSignalRMessage signalRBroadcaster,
                          QualityProfileExistsValidator qualityProfileExistsValidator,
                          MetadataProfileExistsValidator metadataProfileExistsValidator)

        : base(issueService, seriesIssueLinkService, seriesStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
            _seriesService = seriesService;
            _addIssueService = addIssueService;

            PostValidator.RuleFor(s => s.ForeignIssueId).NotEmpty();
            PostValidator.RuleFor(s => s.Series.QualityProfileId).SetValidator(qualityProfileExistsValidator);
            PostValidator.RuleFor(s => s.Series.RootFolderPath).IsValidPath().When(s => s.Series.Path.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.Series.ForeignSeriesId).NotEmpty();
        }

        [HttpGet]
        public List<IssueResource> GetIssues([FromQuery] int? seriesId,
            [FromQuery] List<int> issueIds,
            [FromQuery] string titleSlug,
            [FromQuery] bool includeAllSeriesIssues = false)
        {
            if (!seriesId.HasValue && !issueIds.Any() && titleSlug.IsNullOrWhiteSpace())
            {
                var metadataTask = Task.Run(() => _seriesService.GetAllSeries());
                var issues = _issueService.GetAllIssues();

                var seriesDict = metadataTask.GetAwaiter().GetResult().ToDictionary(x => x.SeriesMetadataId);

                foreach (var issue in issues)
                {
                    issue.Series = seriesDict[issue.SeriesMetadataId];
                }

                return MapToResource(issues, false);
            }

            if (seriesId.HasValue)
            {
                var issues = _issueService.GetIssuesBySeries(seriesId.Value);

                var series = _seriesService.GetSeries(seriesId.Value);

                foreach (var issue in issues)
                {
                    issue.Series = series;
                }

                return MapToResource(issues, false);
            }

            if (titleSlug.IsNotNullOrWhiteSpace())
            {
                var issue = _issueService.FindBySlug(titleSlug);

                if (issue == null)
                {
                    return MapToResource(new List<Issue>(), false);
                }

                if (includeAllSeriesIssues)
                {
                    return MapToResource(_issueService.GetIssuesBySeries(issue.SeriesId), false);
                }
                else
                {
                    return MapToResource(new List<Issue> { issue }, false);
                }
            }

            return MapToResource(_issueService.GetIssues(issueIds), false);
        }

        [HttpGet("{id:int}/overview")]
        public object Overview(int id)
        {
            var issue = _issueService.GetIssue(id);
            return new
            {
                id,
                overview = issue.Title
            };
        }

        [RestPostById]
        public ActionResult<IssueResource> AddIssue([FromBody] IssueResource issueResource)
        {
            var issue = _addIssueService.AddIssue(issueResource.ToModel());

            return Created(issue.Id);
        }

        [RestPutById]
        public ActionResult<IssueResource> UpdateIssue([FromBody] IssueResource issueResource)
        {
            var issue = _issueService.GetIssue(issueResource.Id);

            var model = issueResource.ToModel(issue);

            _issueService.UpdateIssue(model);

            BroadcastResourceChange(ModelAction.Updated, model.Id);

            return Accepted(model.Id);
        }

        [RestDeleteById]
        public void DeleteIssue(int id, bool deleteFiles = false, bool addImportListExclusion = false)
        {
            _issueService.DeleteIssue(id, deleteFiles, addImportListExclusion);
        }

        [NonAction]
        public void Handle(IssueGrabbedEvent message)
        {
            foreach (var issue in message.Issue.Issues)
            {
                var resource = issue.ToResource();
                resource.Grabbed = true;

                BroadcastResourceChange(ModelAction.Updated, resource);
            }
        }

        [NonAction]
        public void Handle(IssueEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Issue, true));
        }

        [NonAction]
        public void Handle(IssueUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Issue, true));
        }

        [NonAction]
        public void Handle(IssueDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, message.Issue.ToResource());
        }

        [NonAction]
        public void Handle(IssueImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.Issue, true));
        }

        [NonAction]
        public void Handle(TrackImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.IssueInfo.Issue.ToResource());
        }

        [NonAction]
        public void Handle(ComicFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.Upgrade)
            {
                return;
            }

            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.ComicFile.Issue.Value, true));
        }
    }
}
