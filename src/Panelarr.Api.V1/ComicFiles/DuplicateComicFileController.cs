using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;
using Panelarr.Http;

namespace Panelarr.Api.V1.ComicFiles
{
    /// <summary>
    /// API endpoints for duplicate ComicFile detection and resolution.
    ///
    /// GET    /api/v1/comicfile/duplicates              → list all duplicate groups
    /// DELETE /api/v1/comicfile/duplicates/{issueId}   → auto-resolve (keep best quality)
    /// PUT    /api/v1/comicfile/duplicates/{issueId}   → manual resolve (caller specifies which file to keep)
    /// </summary>
    [V1ApiController("comicfile/duplicates")]
    public class DuplicateComicFileController : Controller
    {
        private readonly IDuplicateComicFileService _duplicateService;

        public DuplicateComicFileController(IDuplicateComicFileService duplicateService)
        {
            _duplicateService = duplicateService;
        }

        /// <summary>
        /// Returns all issues that have more than one ComicFile, with the preferred (best quality) file indicated.
        /// </summary>
        [HttpGet]
        public List<DuplicateGroupResource> GetDuplicates()
        {
            var groups = _duplicateService.GetAllDuplicates();
            return groups.Select(ToResource).ToList();
        }

        /// <summary>
        /// Auto-resolves duplicates for the given issue: the best-quality file is kept, all others
        /// are sent to the recycle bin and removed from the database.
        /// </summary>
        [HttpDelete("{issueId:int}")]
        public IActionResult AutoResolve(int issueId)
        {
            _duplicateService.AutoResolve(issueId);
            return Ok();
        }

        /// <summary>
        /// Manually resolve duplicates for the given issue by specifying which ComicFile to keep.
        /// All other ComicFiles for that issue are sent to the recycle bin and removed from the database.
        /// Body: { "keepComicFileId": 123 }
        /// </summary>
        [HttpPut("{issueId:int}")]
        public IActionResult ManualResolve(int issueId, [FromBody] ManualResolveRequest body)
        {
            _duplicateService.Resolve(issueId, body.KeepComicFileId);
            return Ok();
        }

        private static DuplicateGroupResource ToResource(DuplicateGroup group)
        {
            return new DuplicateGroupResource
            {
                IssueId = group.IssueId,
                Files = group.Files.Select(f => f.ToResource()).ToList(),
                PreferredComicFileId = group.Preferred?.Id ?? 0
            };
        }
    }

    public class DuplicateGroupResource
    {
        public int IssueId { get; set; }
        public List<ComicFileResource> Files { get; set; }
        public int PreferredComicFileId { get; set; }
    }

    public class ManualResolveRequest
    {
        public int KeepComicFileId { get; set; }
    }
}
