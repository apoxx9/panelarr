using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.IssueImport;
using Panelarr.Http;

namespace Panelarr.Api.V1.ComicImport
{
    /// <summary>
    /// Provides an endpoint to trigger a local comic folder import scan.
    /// GET  /api/v1/comicimport?folder=/path  — preview what would be imported (dry-run decisions)
    /// POST /api/v1/comicimport              — actually import a folder
    /// </summary>
    [V1ApiController]
    public class ComicImportController : Controller
    {
        private readonly IComicImportService _comicImportService;
        private readonly Logger _logger;

        public ComicImportController(IComicImportService comicImportService,
                                      Logger logger)
        {
            _comicImportService = comicImportService;
            _logger = logger;
        }

        /// <summary>
        /// Import all comic files found in the given folder path.
        /// Body: { "folder": "/path/to/comics", "importMode": "Auto" }
        /// </summary>
        [HttpPost]
        public ComicImportResultResource ImportFolder([FromBody] ComicImportRequest request)
        {
            _logger.Info("API: ImportFolder requested for path: {0}", request?.Folder);
            var result = _comicImportService.ImportFolder(
                request?.Folder,
                request?.ImportMode ?? ImportMode.Auto);
            return result.ToResource();
        }
    }

    public class ComicImportRequest
    {
        public string Folder { get; set; }
        public ImportMode ImportMode { get; set; } = ImportMode.Auto;
    }

    public class ComicImportResultResource
    {
        public string Folder { get; set; }
        public int TotalFiles { get; set; }
        public int Imported { get; set; }
        public int Rejected { get; set; }
        public List<string> UnmatchedPaths { get; set; }
        public List<string> Errors { get; set; }
    }

    public static class ComicImportResultMapper
    {
        public static ComicImportResultResource ToResource(this ComicImportResult model)
        {
            if (model == null)
            {
                return null;
            }

            return new ComicImportResultResource
            {
                Folder = model.Folder,
                TotalFiles = model.TotalFiles,
                Imported = model.Imported,
                Rejected = model.Rejected,
                UnmatchedPaths = model.UnmatchedPaths,
                Errors = model.Errors
            };
        }
    }
}
