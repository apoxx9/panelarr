using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Messaging.Commands;
using Panelarr.Http;

namespace Panelarr.Api.V1.Series
{
    [V1ApiController("series/editor")]
    public class SeriesEditorController : Controller
    {
        private readonly ISeriesService _authorService;
        private readonly IManageCommandQueue _commandQueueManager;

        public SeriesEditorController(ISeriesService authorService, IManageCommandQueue commandQueueManager)
        {
            _authorService = authorService;
            _commandQueueManager = commandQueueManager;
        }

        [HttpPut]
        public IActionResult SaveAll([FromBody] SeriesEditorResource resource)
        {
            var authorsToUpdate = _authorService.GetSeriess(resource.SeriesIds);
            var authorsToMove = new List<BulkMoveSeries>();

            foreach (var author in authorsToUpdate)
            {
                if (resource.Monitored.HasValue)
                {
                    author.Monitored = resource.Monitored.Value;
                }

                if (resource.MonitorNewItems.HasValue)
                {
                    author.MonitorNewItems = resource.MonitorNewItems.Value;
                }

                if (resource.QualityProfileId.HasValue)
                {
                    author.QualityProfileId = resource.QualityProfileId.Value;
                }

                if (resource.RootFolderPath.IsNotNullOrWhiteSpace())
                {
                    author.RootFolderPath = resource.RootFolderPath;
                    authorsToMove.Add(new BulkMoveSeries
                    {
                        SeriesId = author.Id,
                        SourcePath = author.Path
                    });
                }

                if (resource.Tags != null)
                {
                    var newTags = resource.Tags;
                    var applyTags = resource.ApplyTags;

                    switch (applyTags)
                    {
                        case ApplyTags.Add:
                            newTags.ForEach(t => author.Tags.Add(t));
                            break;
                        case ApplyTags.Remove:
                            newTags.ForEach(t => author.Tags.Remove(t));
                            break;
                        case ApplyTags.Replace:
                            author.Tags = new HashSet<int>(newTags);
                            break;
                    }
                }
            }

            if (resource.MoveFiles && authorsToMove.Any())
            {
                _commandQueueManager.Push(new BulkMoveSeriesCommand
                {
                    DestinationRootFolder = resource.RootFolderPath,
                    Series = authorsToMove
                });
            }

            return Accepted(_authorService.UpdateSeriess(authorsToUpdate, !resource.MoveFiles).ToResource());
        }

        [HttpDelete]
        public object DeleteSeries([FromBody] SeriesEditorResource resource)
        {
            foreach (var authorId in resource.SeriesIds)
            {
                _authorService.DeleteSeries(authorId, false);
            }

            return new { };
        }
    }
}
