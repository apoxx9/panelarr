using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Metadata;
using Panelarr.Http;

namespace Panelarr.Api.V1.Issues
{
    [V1ApiController("issue/{issueId:int}/override")]
    public class IssueOverrideController : Controller
    {
        private readonly IIssueService _issueService;
        private readonly IMetadataOverrideService _metadataOverrideService;

        public IssueOverrideController(IIssueService issueService,
                                       IMetadataOverrideService metadataOverrideService)
        {
            _issueService = issueService;
            _metadataOverrideService = metadataOverrideService;
        }

        [HttpPut]
        public IActionResult SaveOverride([FromRoute] int issueId, [FromBody] Dictionary<string, object> fields)
        {
            _metadataOverrideService.SaveIssueOverride(issueId, fields);
            return Accepted();
        }

        [HttpDelete]
        public IActionResult ClearOverride([FromRoute] int issueId)
        {
            _metadataOverrideService.ClearIssueOverride(issueId);
            return Ok();
        }
    }
}
