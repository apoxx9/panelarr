using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;
using Panelarr.Http;

namespace Panelarr.Api.V1.Issues
{
    [V1ApiController("rename")]
    public class RenameIssueController : Controller
    {
        private readonly IRenameComicFileService _renameComicFileService;

        public RenameIssueController(IRenameComicFileService renameComicFileService)
        {
            _renameComicFileService = renameComicFileService;
        }

        [HttpGet]
        public List<RenameIssueResource> GetComicFiles(int seriesId, int? issueId)
        {
            if (issueId.HasValue)
            {
                return _renameComicFileService.GetRenamePreviews(seriesId, issueId.Value).ToResource();
            }

            return _renameComicFileService.GetRenamePreviews(seriesId).ToResource();
        }
    }
}
