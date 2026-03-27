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
        private readonly ISeriesGroupService _seriesGroupService;

        public SeriesController(IBroadcastSignalRMessage signalRBroadcaster,
                            ISeriesService authorService,
                            IBookService bookService,
                            IAddSeriesService addSeriesService,
                            ISeriesStatisticsService authorStatisticsService,
                            IMapCoversToLocal coverMapper,
                            IManageCommandQueue commandQueueManager,
                            IRootFolderService rootFolderService,
                            ISeriesGroupService seriesGroupService,
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
            _seriesGroupService = seriesGroupService;

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
            var series = _authorService.GetSeries(id);
            return GetSeriesResource(series);
        }

        private SeriesResource GetSeriesResource(NzbDrone.Core.Books.Series series)
        {
            if (series == null)
            {
                return null;
            }

            var resource = series.ToResource();
            MapCoversToLocal(resource);
            FetchAndLinkSeriesStatistics(resource);
            LinkNextPreviousBooks(resource);

            LinkRootFolderPath(resource);

            return resource;
        }

        [HttpGet]
        public List<SeriesResource> AllSeriess(int? seriesGroupId = null)
        {
            var seriesStats = _authorStatisticsService.SeriesStatistics();
            var allSeries = _authorService.GetAllSeries();

            if (seriesGroupId.HasValue)
            {
                // Filter to series whose issues belong to this SeriesGroup
                var seriesGroup = _seriesGroupService.GetSeriesGroup(seriesGroupId.Value);
                if (seriesGroup != null)
                {
                    var seriesInGroup = _seriesGroupService.GetBySeriesId(seriesGroupId.Value)
                        .Select(sg => sg.Id)
                        .ToHashSet();
                    allSeries = allSeries.Where(s => seriesInGroup.Contains(s.SeriesMetadataId)).ToList();
                }
                else
                {
                    allSeries = new List<global::NzbDrone.Core.Books.Series>();
                }
            }

            var seriesResources = allSeries.ToResource();

            MapCoversToLocal(seriesResources.ToArray());
            LinkNextPreviousBooks(seriesResources.ToArray());
            LinkSeriesStatistics(seriesResources, seriesStats.ToDictionary(x => x.SeriesId));
            LinkRootFolderPath(seriesResources.ToArray());

            return seriesResources;
        }

        [RestPostById]
        public ActionResult<SeriesResource> AddSeries(SeriesResource seriesResource)
        {
            var series = _addSeriesService.AddSeries(seriesResource.ToModel());

            return Created(series.Id);
        }

        [RestPutById]
        public ActionResult<SeriesResource> UpdateSeries(SeriesResource seriesResource, bool moveFiles = false)
        {
            var series = _authorService.GetSeries(seriesResource.Id);

            if (moveFiles)
            {
                var sourcePath = series.Path;
                var destinationPath = seriesResource.Path;

                _commandQueueManager.Push(new MoveSeriesCommand
                {
                    SeriesId = series.Id,
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath,
                    Trigger = CommandTrigger.Manual
                });
            }

            var model = seriesResource.ToModel(series);

            _authorService.UpdateSeries(model);

            BroadcastResourceChange(ModelAction.Updated, seriesResource);

            return Accepted(seriesResource.Id);
        }

        [RestDeleteById]
        public void DeleteSeries(int id, bool deleteFiles = false, bool addImportListExclusion = false)
        {
            _authorService.DeleteSeries(id, deleteFiles, addImportListExclusion);
        }

        private void MapCoversToLocal(params SeriesResource[] seriesList)
        {
            foreach (var seriesResource in seriesList)
            {
                _coverMapper.ConvertToLocalUrls(seriesResource.Id, MediaCoverEntity.Series, seriesResource.Images);
            }
        }

        private void LinkNextPreviousBooks(params SeriesResource[] seriesList)
        {
            var nextBooks = _bookService.GetNextBooksBySeriesMetadataId(seriesList.Select(x => x.SeriesMetadataId));
            var lastBooks = _bookService.GetLastBooksBySeriesMetadataId(seriesList.Select(x => x.SeriesMetadataId));

            foreach (var seriesResource in seriesList)
            {
                seriesResource.NextBook = nextBooks.FirstOrDefault(x => x.SeriesMetadataId == seriesResource.SeriesMetadataId);
                seriesResource.LastBook = lastBooks.FirstOrDefault(x => x.SeriesMetadataId == seriesResource.SeriesMetadataId);
            }
        }

        private void FetchAndLinkSeriesStatistics(SeriesResource resource)
        {
            LinkSeriesStatistics(resource, _authorStatisticsService.SeriesStatistics(resource.Id));
        }

        private void LinkSeriesStatistics(List<SeriesResource> resources, Dictionary<int, SeriesStatistics> seriesStatistics)
        {
            foreach (var series in resources)
            {
                if (seriesStatistics.TryGetValue(series.Id, out var stats))
                {
                    LinkSeriesStatistics(series, stats);
                }
            }
        }

        private void LinkSeriesStatistics(SeriesResource resource, SeriesStatistics seriesStatistics)
        {
            resource.Statistics = seriesStatistics.ToResource();
        }

        private void LinkRootFolderPath(params SeriesResource[] seriesList)
        {
            var rootFolders = _rootFolderService.All();

            foreach (var series in seriesList)
            {
                series.RootFolderPath = _rootFolderService.GetBestRootFolderPath(series.Path, rootFolders);
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
