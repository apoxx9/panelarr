using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
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
        private readonly ISeriesService _authorService;
        private readonly IBookService _bookService;
        private readonly IUpgradableSpecification _upgradableSpecification;

        public ComicFileController(IBroadcastSignalRMessage signalRBroadcaster,
                               IMediaFileService mediaFileService,
                               IDeleteMediaFiles mediaFileDeletionService,
                               IMetadataTagService metadataTagService,
                               ISeriesService authorService,
                               IBookService bookService,
                               IUpgradableSpecification upgradableSpecification)
            : base(signalRBroadcaster)
        {
            _mediaFileService = mediaFileService;
            _mediaFileDeletionService = mediaFileDeletionService;
            _metadataTagService = metadataTagService;
            _authorService = authorService;
            _bookService = bookService;
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
        public List<ComicFileResource> GetBookFiles(int? authorId, [FromQuery]List<int> bookFileIds, [FromQuery(Name="bookId")]List<int> bookIds, bool? unmapped)
        {
            if (!authorId.HasValue && !bookFileIds.Any() && !bookIds.Any() && !unmapped.HasValue)
            {
                throw new BadRequestException("authorId, bookId, bookFileIds or unmapped must be provided");
            }

            if (unmapped.HasValue && unmapped.Value)
            {
                var files = _mediaFileService.GetUnmappedFiles();
                return files.ConvertAll(f => MapToResource(f));
            }

            if (authorId.HasValue && !bookIds.Any())
            {
                var author = _authorService.GetSeries(authorId.Value);

                return _mediaFileService.GetFilesBySeries(authorId.Value).ConvertAll(f => f.ToResource(author, _upgradableSpecification));
            }

            if (bookIds.Any())
            {
                var result = new List<ComicFileResource>();
                foreach (var bookId in bookIds)
                {
                    var issue = _bookService.GetBook(bookId);
                    var bookSeries = _authorService.GetSeries(issue.SeriesId);
                    result.AddRange(_mediaFileService.GetFilesByBook(issue.Id).ConvertAll(f => f.ToResource(bookSeries, _upgradableSpecification)));
                }

                return result;
            }
            else
            {
                // trackfiles will come back with the author already populated
                var comicFiles = _mediaFileService.Get(bookFileIds);
                return comicFiles.ConvertAll(e => MapToResource(e));
            }
        }

        [RestPutById]
        public ActionResult<ComicFileResource> SetQuality(ComicFileResource bookFileResource)
        {
            var comicFile = _mediaFileService.Get(bookFileResource.Id);
            comicFile.Quality = bookFileResource.Quality;
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
        public void DeleteBookFile(int id)
        {
            var comicFile = _mediaFileService.Get(id);

            if (comicFile == null)
            {
                throw new NzbDroneClientException(HttpStatusCode.NotFound, "Issue file not found");
            }

            if (comicFile.IssueId > 0 && comicFile.Series != null && comicFile.Series.Value != null)
            {
                _mediaFileDeletionService.DeleteTrackFile(comicFile.Series.Value, comicFile);
            }
            else
            {
                _mediaFileDeletionService.DeleteTrackFile(comicFile, "Unmapped_Files");
            }
        }

        [HttpDelete("bulk")]
        public object DeleteTrackFiles([FromBody] ComicFileListResource resource)
        {
            var comicFiles = _mediaFileService.Get(resource.ComicFileIds);

            foreach (var comicFile in comicFiles)
            {
                if (comicFile.IssueId > 0 && comicFile.Series != null && comicFile.Series.Value != null)
                {
                    _mediaFileDeletionService.DeleteTrackFile(comicFile.Series.Value, comicFile);
                }
                else
                {
                    _mediaFileDeletionService.DeleteTrackFile(comicFile, "Unmapped_Files");
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
