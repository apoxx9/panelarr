using System.Collections.Generic;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Http.REST.Attributes;
using Panelarr.Http;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.ImportLists
{
    [V1ApiController]
    public class ImportListExclusionController : RestController<ImportListExclusionResource>
    {
        private readonly IImportListExclusionService _importListExclusionService;

        public ImportListExclusionController(IImportListExclusionService importListExclusionService,
                                         ImportListExclusionExistsValidator importListExclusionExistsValidator)
        {
            _importListExclusionService = importListExclusionService;

            // Comic foreign ids (cv-*, metron ids) are not GUIDs — no GuidValidator here.
            SharedValidator.RuleFor(c => c.ForeignId).NotEmpty().SetValidator(importListExclusionExistsValidator);
            SharedValidator.RuleFor(c => c.SeriesName).NotEmpty();
        }

        protected override ImportListExclusionResource GetResourceById(int id)
        {
            return _importListExclusionService.Get(id).ToResource();
        }

        [HttpGet]
        public List<ImportListExclusionResource> GetImportListExclusions()
        {
            return _importListExclusionService.All().ToResource();
        }

        [RestPostById]
        public ActionResult<ImportListExclusionResource> AddImportListExclusion([FromBody] ImportListExclusionResource resource)
        {
            var customFilter = _importListExclusionService.Add(resource.ToModel());

            return Created(customFilter.Id);
        }

        [RestPutById]
        public ActionResult<ImportListExclusionResource> UpdateImportListExclusion([FromBody] ImportListExclusionResource resource)
        {
            _importListExclusionService.Update(resource.ToModel());
            return Accepted(resource.Id);
        }

        [RestDeleteById]
        public void DeleteImportListExclusionResource(int id)
        {
            _importListExclusionService.Delete(id);
        }
    }
}
