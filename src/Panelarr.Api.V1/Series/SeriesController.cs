using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Commands;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.SeriesStats;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;
using NzbDrone.Http.REST.Attributes;
using NzbDrone.SignalR;
using Panelarr.Http;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Series
{
    [V1ApiController]
    public class SeriesController : RestControllerWithSignalR<SeriesResource, NzbDrone.Core.Books.Series>,
                                IHandle<IssueImportedEvent>,
                                IHandle<IssueEditedEvent>,
                                IHandle<ComicFileDeletedEvent>,
                                IHandle<SeriesAddedEvent>,
                                IHandle<SeriesUpdatedEvent>,
                                IHandle<SeriesEditedEvent>,
                                IHandle<SeriesDeletedEvent>,
                                IHandle<SeriesRenamedEvent>,
                                IHandle<MediaCoversUpdatedEvent>
    {
        private readonly ISeriesService _authorService;
        private readonly IBookService _bookService;
        private readonly IAddSeriesService _addSeriesService;
        private readonly ISeriesStatisticsService _authorStatisticsService;
        private readonly IMapCoversToLocal _coverMapper;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IRootFolderService _rootFolderService;

        public SeriesController(IBroadcastSignalRMessage signalRBroadcaster,
                            ISeriesService authorService,
                            IBookService bookService,
                            IAddSeriesService addSeriesService,
                            ISeriesStatisticsService authorStatisticsService,
                            IMapCoversToLocal coverMapper,
                            IManageCommandQueue commandQueueManager,
                            IRootFolderService rootFolderService,
                            RecycleBinValidator recycleBinValidator,
                            RootFolderValidator rootFolderValidator,
                            MappedNetworkDriveValidator mappedNetworkDriveValidator,
                            SeriesPathValidator authorPathValidator,
                            SeriesExistsValidator authorExistsValidator,
                            SeriesAncestorValidator authorAncestorValidator,
                            SystemFolderValidator systemFolderValidator,
                            QualityProfileExistsValidator qualityProfileExistsValidator,
                            MetadataProfileExistsValidator metadataProfileExistsValidator,
                            SeriesFolderAsRootFolderValidator authorFolderAsRootFolderValidator)
            : base(signalRBroadcaster)
        {
            _authorService = authorService;
            _bookService = bookService;
            _addSeriesService = addSeriesService;
            _authorStatisticsService = authorStatisticsService;

            _coverMapper = coverMapper;
            _commandQueueManager = commandQueueManager;
            _rootFolderService = rootFolderService;

            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.QualityProfileId));

            SharedValidator.RuleFor(s => s.Path)
                           .Cascade(CascadeMode.Stop)
                           .IsValidPath()
                           .SetValidator(rootFolderValidator)
                           .SetValidator(mappedNetworkDriveValidator)
                           .SetValidator(authorPathValidator)
                           .SetValidator(authorAncestorValidator)
                           .SetValidator(recycleBinValidator)
                           .SetValidator(systemFolderValidator)
                           .When(s => !s.Path.IsNullOrWhiteSpace());

            SharedValidator.RuleFor(s => s.QualityProfileId).SetValidator(qualityProfileExistsValidator);

            PostValidator.RuleFor(s => s.Path).IsValidPath().When(s => s.RootFolderPath.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.RootFolderPath)
                         .IsValidPath()
                         .SetValidator(authorFolderAsRootFolderValidator)
                         .When(s => s.Path.IsNullOrWhiteSpace());
            PostValidator.RuleFor(s => s.SeriesName).NotEmpty();
            PostValidator.RuleFor(s => s.ForeignSeriesId).NotEmpty().SetValidator(authorExistsValidator);

            PutValidator.RuleFor(s => s.Path).IsValidPath();
        }

        protected override SeriesResource GetResourceById(int id)
        {
            var author = _authorService.GetSeries(id);
            return GetSeriesResource(author);
        }

        private SeriesResource GetSeriesResource(NzbDrone.Core.Books.Series author)
        {
            if (author == null)
            {
                return null;
            }

            var resource = author.ToResource();
            MapCoversToLocal(resource);
            FetchAndLinkSeriesStatistics(resource);
            LinkNextPreviousBooks(resource);

            LinkRootFolderPath(resource);

            return resource;
        }

        [HttpGet]
        public List<SeriesResource> AllSeriess()
        {
            var authorStats = _authorStatisticsService.SeriesStatistics();
            var authorResources = _authorService.GetAllSeries().ToResource();

            MapCoversToLocal(authorResources.ToArray());
            LinkNextPreviousBooks(authorResources.ToArray());
            LinkSeriesStatistics(authorResources, authorStats.ToDictionary(x => x.SeriesId));
            LinkRootFolderPath(authorResources.ToArray());

            return authorResources;
        }

        [RestPostById]
        public ActionResult<SeriesResource> AddSeries(SeriesResource authorResource)
        {
            var author = _addSeriesService.AddSeries(authorResource.ToModel());

            return Created(author.Id);
        }

        [RestPutById]
        public ActionResult<SeriesResource> UpdateSeries(SeriesResource authorResource, bool moveFiles = false)
        {
            var author = _authorService.GetSeries(authorResource.Id);

            if (moveFiles)
            {
                var sourcePath = author.Path;
                var destinationPath = authorResource.Path;

                _commandQueueManager.Push(new MoveSeriesCommand
                {
                    SeriesId = author.Id,
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath,
                    Trigger = CommandTrigger.Manual
                });
            }

            var model = authorResource.ToModel(author);

            _authorService.UpdateSeries(model);

            BroadcastResourceChange(ModelAction.Updated, authorResource);

            return Accepted(authorResource.Id);
        }

        [RestDeleteById]
        public void DeleteSeries(int id, bool deleteFiles = false, bool addImportListExclusion = false)
        {
            _authorService.DeleteSeries(id, deleteFiles, addImportListExclusion);
        }

        private void MapCoversToLocal(params SeriesResource[] authors)
        {
            foreach (var authorResource in authors)
            {
                _coverMapper.ConvertToLocalUrls(authorResource.Id, MediaCoverEntity.Series, authorResource.Images);
            }
        }

        private void LinkNextPreviousBooks(params SeriesResource[] authors)
        {
            var nextBooks = _bookService.GetNextBooksBySeriesMetadataId(authors.Select(x => x.SeriesMetadataId));
            var lastBooks = _bookService.GetLastBooksBySeriesMetadataId(authors.Select(x => x.SeriesMetadataId));

            foreach (var authorResource in authors)
            {
                authorResource.NextBook = nextBooks.FirstOrDefault(x => x.SeriesMetadataId == authorResource.SeriesMetadataId);
                authorResource.LastBook = lastBooks.FirstOrDefault(x => x.SeriesMetadataId == authorResource.SeriesMetadataId);
            }
        }

        private void FetchAndLinkSeriesStatistics(SeriesResource resource)
        {
            LinkSeriesStatistics(resource, _authorStatisticsService.SeriesStatistics(resource.Id));
        }

        private void LinkSeriesStatistics(List<SeriesResource> resources, Dictionary<int, SeriesStatistics> authorStatistics)
        {
            foreach (var author in resources)
            {
                if (authorStatistics.TryGetValue(author.Id, out var stats))
                {
                    LinkSeriesStatistics(author, stats);
                }
            }
        }

        private void LinkSeriesStatistics(SeriesResource resource, SeriesStatistics authorStatistics)
        {
            resource.Statistics = authorStatistics.ToResource();
        }

        private void LinkRootFolderPath(params SeriesResource[] authors)
        {
            var rootFolders = _rootFolderService.All();

            foreach (var author in authors)
            {
                author.RootFolderPath = _rootFolderService.GetBestRootFolderPath(author.Path, rootFolders);
            }
        }

        [NonAction]
        public void Handle(IssueImportedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetSeriesResource(message.Series));
        }

        [NonAction]
        public void Handle(IssueEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetSeriesResource(message.Issue.Series.Value));
        }

        [NonAction]
        public void Handle(ComicFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.Upgrade)
            {
                return;
            }

            BroadcastResourceChange(ModelAction.Updated, GetSeriesResource(message.ComicFile.Series.Value));
        }

        [NonAction]
        public void Handle(SeriesAddedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetSeriesResource(message.Series));
        }

        [NonAction]
        public void Handle(SeriesUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetSeriesResource(message.Series));
        }

        [NonAction]
        public void Handle(SeriesEditedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetSeriesResource(message.Series));
        }

        [NonAction]
        public void Handle(SeriesDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, message.Series.ToResource());
        }

        [NonAction]
        public void Handle(SeriesRenamedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, message.Series.Id);
        }

        [NonAction]
        public void Handle(MediaCoversUpdatedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, GetSeriesResource(message.Series));
        }
    }
}
