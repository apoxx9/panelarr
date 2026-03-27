using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
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

namespace Panelarr.Api.V1.Books
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
        protected readonly ISeriesService _authorService;
        protected readonly IAddBookService _addBookService;

        public IssueController(ISeriesService authorService,
                          IBookService bookService,
                          IAddBookService addBookService,
                          ISeriesBookLinkService seriesBookLinkService,
                          ISeriesStatisticsService authorStatisticsService,
                          IMapCoversToLocal coverMapper,
                          IUpgradableSpecification upgradableSpecification,
                          IBroadcastSignalRMessage signalRBroadcaster,
                          QualityProfileExistsValidator qualityProfileExistsValidator,
                          MetadataProfileExistsValidator metadataProfileExistsValidator)

        : base(bookService, seriesBookLinkService, authorStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
            _authorService = authorService;
            _addBookService = addBookService;

            PostValidator.RuleFor(s => s.ForeignIssueId).NotEmpty();
            PostValidator.RuleFor(s => s.Series.QualityProfileId).SetValidator(qualityProfileExistsValidator);
            PostValidator.RuleFor(s => s.Series.RootFolderPath).IsValidPath().When(s => s.Series.Path.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.Series.ForeignSeriesId).NotEmpty();
        }

        [HttpGet]
        public List<IssueResource> GetBooks([FromQuery]int? seriesId,
            [FromQuery]List<int> issueIds,
            [FromQuery]string titleSlug,
            [FromQuery]bool includeAllSeriesBooks = false)
        {
            if (!seriesId.HasValue && !issueIds.Any() && titleSlug.IsNullOrWhiteSpace())
            {
                var metadataTask = Task.Run(() => _authorService.GetAllSeries());
                var issues = _bookService.GetAllBooks();

                var seriesDict = metadataTask.GetAwaiter().GetResult().ToDictionary(x => x.SeriesMetadataId);

                foreach (var issue in issues)
                {
                    issue.Series = seriesDict[issue.SeriesMetadataId];
                }

                return MapToResource(issues, false);
            }

            if (seriesId.HasValue)
            {
                var issues = _bookService.GetBooksBySeries(seriesId.Value);

                var series = _authorService.GetSeries(seriesId.Value);

                foreach (var issue in issues)
                {
                    issue.Series = series;
                }

                return MapToResource(issues, false);
            }

            if (titleSlug.IsNotNullOrWhiteSpace())
            {
                var issue = _bookService.FindBySlug(titleSlug);

                if (issue == null)
                {
                    return MapToResource(new List<Issue>(), false);
                }

                if (includeAllSeriesBooks)
                {
                    return MapToResource(_bookService.GetBooksBySeries(issue.SeriesId), false);
                }
                else
                {
                    return MapToResource(new List<Issue> { issue }, false);
                }
            }

            return MapToResource(_bookService.GetBooks(issueIds), false);
        }

        [HttpGet("{id:int}/overview")]
        public object Overview(int id)
        {
            var issue = _bookService.GetBook(id);
            return new
            {
                id,
                overview = issue.Title
            };
        }

        [RestPostById]
        public ActionResult<IssueResource> AddBook(IssueResource bookResource)
        {
            var issue = _addBookService.AddBook(bookResource.ToModel());

            return Created(issue.Id);
        }

        [RestPutById]
        public ActionResult<IssueResource> UpdateBook(IssueResource bookResource)
        {
            var issue = _bookService.GetBook(bookResource.Id);

            var model = bookResource.ToModel(issue);

            _bookService.UpdateBook(model);

            BroadcastResourceChange(ModelAction.Updated, model.Id);

            return Accepted(model.Id);
        }

        [RestDeleteById]
        public void DeleteBook(int id, bool deleteFiles = false, bool addImportListExclusion = false)
        {
            _bookService.DeleteBook(id, deleteFiles, addImportListExclusion);
        }

        [NonAction]
        public void Handle(IssueGrabbedEvent message)
        {
            foreach (var issue in message.Issue.Books)
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
            BroadcastResourceChange(ModelAction.Updated, message.BookInfo.Issue.ToResource());
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
