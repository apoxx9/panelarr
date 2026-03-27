using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;
using Panelarr.Http;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Books
{
    [V1ApiController("retag")]
    public class RetagBookController : Controller
    {
        private readonly IMetadataTagService _metadataTagService;

        public RetagBookController(IMetadataTagService metadataTagService)
        {
            _metadataTagService = metadataTagService;
        }

        [HttpGet]
        public List<RetagIssueResource> GetBooks(int? seriesId, int? issueId)
        {
            if (issueId.HasValue)
            {
                return _metadataTagService.GetRetagPreviewsByBook(issueId.Value).Where(x => x.Changes.Any()).ToResource();
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
