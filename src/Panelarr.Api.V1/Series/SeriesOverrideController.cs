using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Metadata;
using Panelarr.Http;

namespace Panelarr.Api.V1.Series
{
    [V1ApiController("series/{seriesId:int}/override")]
    public class SeriesOverrideController : Controller
    {
        private readonly ISeriesService _seriesService;
        private readonly IMetadataOverrideService _metadataOverrideService;

        public SeriesOverrideController(ISeriesService seriesService,
                                        IMetadataOverrideService metadataOverrideService)
        {
            _seriesService = seriesService;
            _metadataOverrideService = metadataOverrideService;
        }

        [HttpPut]
        public IActionResult SaveOverride([FromRoute] int seriesId, [FromBody] Dictionary<string, object> fields)
        {
            var series = _seriesService.GetSeries(seriesId);
            _metadataOverrideService.SaveSeriesOverride(series.SeriesMetadataId, fields);
            return Accepted();
        }

        [HttpDelete]
        public IActionResult ClearOverride([FromRoute] int seriesId)
        {
            var series = _seriesService.GetSeries(seriesId);
            _metadataOverrideService.ClearSeriesOverride(series.SeriesMetadataId);
            return Ok();
        }
    }
}
