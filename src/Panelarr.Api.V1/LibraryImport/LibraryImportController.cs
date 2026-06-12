using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles.LibraryImport;
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

        public LibraryImportController(ILibraryImportProposalService proposalService)
        {
            _proposalService = proposalService;
        }

        [HttpGet("proposal")]
        public List<LibraryImportProposalResource> GetProposals(int rootFolderId)
        {
            return _proposalService.GetProposals(rootFolderId)
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
