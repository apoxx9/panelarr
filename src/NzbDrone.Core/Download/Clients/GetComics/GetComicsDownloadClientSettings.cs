using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Download.Clients.GetComics
{
    public class GetComicsDownloadClientSettingsValidator : AbstractValidator<GetComicsDownloadClientSettings>
    {
        public GetComicsDownloadClientSettingsValidator()
        {
            RuleFor(c => c.DownloadFolder).IsValidPath();
        }
    }

    public class GetComicsDownloadClientSettings : IProviderConfig
    {
        private static readonly GetComicsDownloadClientSettingsValidator Validator = new GetComicsDownloadClientSettingsValidator();

        [FieldDefinition(0, Label = "Download Folder", Type = FieldType.Path, HelpText = "Folder where completed comic downloads will be placed for import")]
        public string DownloadFolder { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
