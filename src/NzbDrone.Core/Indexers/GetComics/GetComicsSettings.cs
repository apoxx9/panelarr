using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.GetComics
{
    public class GetComicsSettingsValidator : AbstractValidator<GetComicsSettings>
    {
        public GetComicsSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
        }
    }

    public class GetComicsSettings : IIndexerSettings
    {
        private static readonly GetComicsSettingsValidator Validator = new GetComicsSettingsValidator();

        public GetComicsSettings()
        {
            BaseUrl = "https://getcomics.org";
        }

        [FieldDefinition(0, Label = "Website URL")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Type = FieldType.Number, Label = "Early Download Limit", Unit = "days", HelpText = "Time before release date Panelarr will download from this indexer, empty is no limit", Advanced = true)]
        public int? EarlyReleaseLimit { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
