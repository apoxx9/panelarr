using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.ComicInfo;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Http.REST.Attributes;
using NzbDrone.SignalR;
using Panelarr.Http;
using Panelarr.Http.REST;
using BadRequestException = NzbDrone.Core.Exceptions.BadRequestException;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Panelarr.Api.V1.ComicFiles
{
    [V1ApiController]
    public class ComicFileController : RestControllerWithSignalR<ComicFileResource, ComicFile>,
                                 IHandle<ComicFileAddedEvent>,
                                 IHandle<ComicFileDeletedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IDeleteMediaFiles _mediaFileDeletionService;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IComicInfoReaderService _comicInfoReaderService;
        private readonly ISeriesService _seriesService;
        private readonly IIssueService _issueService;
        private readonly IUpgradableSpecification _upgradableSpecification;

        public ComicFileController(IBroadcastSignalRMessage signalRBroadcaster,
                               IMediaFileService mediaFileService,
                               IDeleteMediaFiles mediaFileDeletionService,
                               IMetadataTagService metadataTagService,
                               IComicInfoReaderService comicInfoReaderService,
                               ISeriesService seriesService,
                               IIssueService issueService,
                               IUpgradableSpecification upgradableSpecification)
            : base(signalRBroadcaster)
        {
            _mediaFileService = mediaFileService;
            _mediaFileDeletionService = mediaFileDeletionService;
            _metadataTagService = metadataTagService;
            _comicInfoReaderService = comicInfoReaderService;
            _seriesService = seriesService;
            _issueService = issueService;
            _upgradableSpecification = upgradableSpecification;
        }

        private ComicFileResource MapToResource(ComicFile comicFile)
        {
            if (comicFile.IssueId > 0 && comicFile.Series != null && comicFile.Series.Value != null)
            {
                return comicFile.ToResource(comicFile.Series.Value, _upgradableSpecification);
            }
            else
            {
                return comicFile.ToResource();
            }
        }

        protected override ComicFileResource GetResourceById(int id)
        {
            var resource = MapToResource(_mediaFileService.Get(id));
            resource.AudioTags = _metadataTagService.ReadTags((FileInfoBase)new FileInfo(resource.Path));
            return resource;
        }

        [HttpGet]
        public List<ComicFileResource> GetComicFiles(int? seriesId, [FromQuery] List<int> issueFileIds, [FromQuery(Name = "issueId")] List<int> issueIds, bool? unmapped)
        {
            if (!seriesId.HasValue && !issueFileIds.Any() && !issueIds.Any() && !unmapped.HasValue)
            {
                throw new BadRequestException("seriesId, issueId, issueFileIds or unmapped must be provided");
            }

            if (unmapped.HasValue && unmapped.Value)
            {
                var files = _mediaFileService.GetUnmappedFiles();
                return files.ConvertAll(f => MapToResource(f));
            }

            if (seriesId.HasValue && !issueIds.Any())
            {
                var series = _seriesService.GetSeries(seriesId.Value);

                return _mediaFileService.GetFilesBySeries(seriesId.Value).ConvertAll(f => f.ToResource(series, _upgradableSpecification));
            }

            if (issueIds.Any())
            {
                var result = new List<ComicFileResource>();
                foreach (var issueId in issueIds)
                {
                    var issue = _issueService.GetIssue(issueId);
                    var issueSeries = _seriesService.GetSeries(issue.SeriesId);
                    result.AddRange(_mediaFileService.GetFilesByIssue(issue.Id).ConvertAll(f => f.ToResource(issueSeries, _upgradableSpecification)));
                }

                return result;
            }
            else
            {
                // trackfiles will come back with the series already populated
                var comicFiles = _mediaFileService.Get(issueFileIds);
                return comicFiles.ConvertAll(e => MapToResource(e));
            }
        }

        [HttpGet("{id:int}/metadata")]
        public ActionResult<List<ComicMetadataResult>> GetMetadata(int id)
        {
            var comicFile = _mediaFileService.Get(id);
            if (comicFile == null)
            {
                return NotFound();
            }

            var metadata = _comicInfoReaderService.ReadMetadata(comicFile);
            return Ok(metadata);
        }

        [RestPutById]
        public ActionResult<ComicFileResource> SetQuality([FromBody] ComicFileResource comicFileResource)
        {
            var comicFile = _mediaFileService.Get(comicFileResource.Id);
            comicFile.Quality = comicFileResource.Quality;
            _mediaFileService.Update(comicFile);
            return Accepted(comicFile.Id);
        }

        [HttpPut("editor")]
        public IActionResult SetQuality([FromBody] ComicFileListResource resource)
        {
            var comicFiles = _mediaFileService.Get(resource.ComicFileIds);

            foreach (var comicFile in comicFiles)
            {
                if (resource.Quality != null)
                {
                    comicFile.Quality = resource.Quality;
                }
            }

            _mediaFileService.Update(comicFiles);

            return Accepted(comicFiles.ConvertAll(f => f.ToResource(comicFiles.First().Series.Value, _upgradableSpecification)));
        }

        [RestDeleteById]
        public void DeleteComicFile(int id)
        {
            var comicFile = _mediaFileService.Get(id);

            if (comicFile == null)
            {
                throw new NzbDroneClientException(HttpStatusCode.NotFound, "Issue file not found");
            }

            if (comicFile.IssueId > 0 && comicFile.Series != null && comicFile.Series.Value != null)
            {
                _mediaFileDeletionService.DeleteComicFile(comicFile.Series.Value, comicFile);
            }
            else
            {
                _mediaFileDeletionService.DeleteComicFile(comicFile, "Unmapped_Files");
            }
        }

        [HttpDelete("bulk")]
        public object DeleteComicFiles([FromBody] ComicFileListResource resource)
        {
            var comicFiles = _mediaFileService.Get(resource.ComicFileIds);

            foreach (var comicFile in comicFiles)
            {
                if (comicFile.IssueId > 0 && comicFile.Series != null && comicFile.Series.Value != null)
                {
                    _mediaFileDeletionService.DeleteComicFile(comicFile.Series.Value, comicFile);
                }
                else
                {
                    _mediaFileDeletionService.DeleteComicFile(comicFile, "Unmapped_Files");
                }
            }

            return new { };
        }

        [NonAction]
        public void Handle(ComicFileAddedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.ComicFile));
        }

        [NonAction]
        public void Handle(ComicFileDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, MapToResource(message.ComicFile));
        }
    }
}
