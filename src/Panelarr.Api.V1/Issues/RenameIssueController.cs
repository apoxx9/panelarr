using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;
using Panelarr.Http;

namespace Panelarr.Api.V1.Books
{
    [V1ApiController("rename")]
    public class RenameBookController : Controller
    {
        private readonly IRenameBookFileService _renameBookFileService;

        public RenameBookController(IRenameBookFileService renameBookFileService)
        {
            _renameBookFileService = renameBookFileService;
        }

        [HttpGet]
        public List<RenameIssueResource> GetBookFiles(int seriesId, int? issueId)
        {
            if (issueId.HasValue)
            {
                return _renameBookFileService.GetRenamePreviews(seriesId, issueId.Value).ToResource();
            }

            return _renameBookFileService.GetRenamePreviews(seriesId).ToResource();
        }
    }
}
