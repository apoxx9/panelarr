using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;
using Panelarr.Http;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Issues
{
    [V1ApiController("retag")]
    public class RetagIssueController : Controller
    {
        private readonly IMetadataTagService _metadataTagService;

        public RetagIssueController(IMetadataTagService metadataTagService)
        {
            _metadataTagService = metadataTagService;
        }

        [HttpGet]
        public List<RetagIssueResource> GetIssues(int? seriesId, int? issueId)
        {
            if (issueId.HasValue)
            {
                return _metadataTagService.GetRetagPreviewsByIssue(issueId.Value).Where(x => x.Changes.Any()).ToResource();
            }
            else if (seriesId.HasValue)
            {
                return _metadataTagService.GetRetagPreviewsBySeries(seriesId.Value).Where(x => x.Changes.Any()).ToResource();
            }
            else
            {
                throw new BadRequestException("One of seriesId or issueId must be specified");
            }
        }
    }
}
