using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;
using NzbDrone.Http.REST.Attributes;
using Panelarr.Http;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.SeriesGroups
{
    [V1ApiController("seriesgroup")]
    public class SeriesGroupController : RestController<SeriesGroupResource>
    {
        private readonly ISeriesGroupService _seriesGroupService;

        public SeriesGroupController(ISeriesGroupService seriesGroupService)
        {
            _seriesGroupService = seriesGroupService;
        }

        protected override SeriesGroupResource GetResourceById(int id)
        {
            return _seriesGroupService.GetSeriesGroup(id).ToResource();
        }

        [HttpGet]
        public List<SeriesGroupResource> GetAllSeriesGroups()
        {
            return _seriesGroupService.GetAllSeriesGroups().ToResource();
        }

        [RestPostById]
        public ActionResult<SeriesGroupResource> CreateSeriesGroup([FromBody] SeriesGroupResource resource)
        {
            var seriesGroup = _seriesGroupService.AddSeriesGroup(resource.ToModel());
            return Created(seriesGroup.Id);
        }

        [RestPutById]
        public ActionResult<SeriesGroupResource> UpdateSeriesGroup([FromBody] SeriesGroupResource resource)
        {
            var seriesGroup = _seriesGroupService.UpdateSeriesGroup(resource.ToModel());
            return Accepted(seriesGroup.ToResource());
        }

        [RestDeleteById]
        public void DeleteSeriesGroup(int id)
        {
            _seriesGroupService.Delete(id);
        }
    }
}
