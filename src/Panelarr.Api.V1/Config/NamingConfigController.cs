using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Organizer;
using NzbDrone.Http.REST.Attributes;
using Panelarr.Http;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Config
{
    [V1ApiController("config/naming")]
    public class NamingConfigController : RestController<NamingConfigResource>
    {
        private readonly INamingConfigService _namingConfigService;
        private readonly IFilenameSampleService _filenameSampleService;
        private readonly IFilenameValidationService _filenameValidationService;
        private readonly IBuildFileNames _filenameBuilder;

        public NamingConfigController(INamingConfigService namingConfigService,
                                  IFilenameSampleService filenameSampleService,
                                  IFilenameValidationService filenameValidationService,
                                  IBuildFileNames filenameBuilder)
        {
            _namingConfigService = namingConfigService;
            _filenameSampleService = filenameSampleService;
            _filenameValidationService = filenameValidationService;
            _filenameBuilder = filenameBuilder;

            SharedValidator.RuleFor(c => c.StandardIssueFormat).ValidIssueFormat();
            SharedValidator.RuleFor(c => c.SeriesFolderFormat).ValidSeriesFolderFormat();
        }

        protected override NamingConfigResource GetResourceById(int id)
        {
            return GetNamingConfig();
        }

        [HttpGet]
        public NamingConfigResource GetNamingConfig()
        {
            var nameSpec = _namingConfigService.GetConfig();
            var resource = nameSpec.ToResource();

            if (resource.StandardIssueFormat.IsNotNullOrWhiteSpace())
            {
                var basicConfig = _filenameBuilder.GetBasicNamingConfig(nameSpec);
                basicConfig.AddToResource(resource);
            }

            return resource;
        }

        [RestPutById]
        public ActionResult<NamingConfigResource> UpdateNamingConfig([FromBody] NamingConfigResource resource)
        {
            var nameSpec = resource.ToModel();
            ValidateFormatResult(nameSpec);

            _namingConfigService.Save(nameSpec);

            return Accepted(resource.Id);
        }

        [HttpGet("examples")]
        public object GetExamples([FromQuery] NamingConfigResource config)
        {
            if (config.Id == 0)
            {
                config = GetNamingConfig();
            }

            var nameSpec = config.ToModel();
            var sampleResource = new NamingExampleResource();

            var singleIssueSampleResult = _filenameSampleService.GetStandardIssueSample(nameSpec);
            var multiDiscIssueSampleResult = _filenameSampleService.GetMultiDiscIssueSample(nameSpec);

            sampleResource.SingleIssueExample = _filenameValidationService.ValidateComicFilename(singleIssueSampleResult) != null
                    ? null
                    : singleIssueSampleResult.FileName;

            sampleResource.MultiPartIssueExample = _filenameValidationService.ValidateComicFilename(multiDiscIssueSampleResult) != null
                ? null
                : multiDiscIssueSampleResult.FileName;

            sampleResource.SeriesFolderExample = nameSpec.SeriesFolderFormat.IsNullOrWhiteSpace()
                ? null
                : _filenameSampleService.GetSeriesFolderSample(nameSpec);

            return sampleResource;
        }

        private void ValidateFormatResult(NamingConfig nameSpec)
        {
            var singleIssueSampleResult = _filenameSampleService.GetStandardIssueSample(nameSpec);

            var singleIssueValidationResult = _filenameValidationService.ValidateComicFilename(singleIssueSampleResult);

            var validationFailures = new List<ValidationFailure>();

            validationFailures.AddIfNotNull(singleIssueValidationResult);

            if (validationFailures.Any())
            {
                throw new ValidationException(validationFailures.DistinctBy(v => v.PropertyName).ToArray());
            }
        }
    }
}
