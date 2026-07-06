using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles.LibraryImport;
using NzbDrone.Core.RootFolders;
using Panelarr.Http;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.LibraryImport
{
    public class LibraryImportProposalResource : RestResource
    {
        public string Folder { get; set; }
        public string ForeignSeriesId { get; set; }
        public string Name { get; set; }
        public int? Year { get; set; }
        public string Confidence { get; set; }
        public string IdSource { get; set; }
        public int FileCount { get; set; }
        public int? ExistingSeriesId { get; set; }
    }

    [V1ApiController("libraryimport")]
    public class LibraryImportController : Controller
    {
        private readonly ILibraryImportProposalService _proposalService;
        private readonly IRootFolderService _rootFolderService;

        public LibraryImportController(ILibraryImportProposalService proposalService,
                                       IRootFolderService rootFolderService)
        {
            _proposalService = proposalService;
            _rootFolderService = rootFolderService;
        }

        [HttpGet("proposal")]
        public List<LibraryImportProposalResource> GetProposals(int? rootFolderId, string folder)
        {
            if (rootFolderId.HasValue == folder.IsNotNullOrWhiteSpace())
            {
                throw new BadRequestException("Either rootFolderId or folder must be provided, not both");
            }

            List<LibraryImportProposal> proposals;

            if (rootFolderId.HasValue)
            {
                proposals = _proposalService.GetProposals(rootFolderId.Value);
            }
            else
            {
                // Staging scan: in-place import owns everything inside the
                // library, so a staging folder must live outside all roots.
                if (_rootFolderService.All().Any(r => folder.PathEquals(r.Path) || r.Path.IsParentPath(folder)))
                {
                    throw new BadRequestException("Folder is inside a root folder; use the library import scan instead");
                }

                proposals = _proposalService.GetProposalsForFolder(folder);
            }

            return proposals
                .Select((p, i) => new LibraryImportProposalResource
                {
                    Id = i + 1,
                    Folder = p.Folder,
                    ForeignSeriesId = p.ForeignSeriesId,
                    Name = p.Name,
                    Year = p.Year,
                    Confidence = p.Confidence.ToString().ToLowerInvariant(),
                    IdSource = p.IdSource,
                    FileCount = p.FileCount,
                    ExistingSeriesId = p.ExistingSeriesId
                })
                .ToList();
        }
    }
}
