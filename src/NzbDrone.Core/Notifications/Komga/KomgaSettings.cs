using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Komga
{
    public class KomgaSettingsValidator : AbstractValidator<KomgaSettings>
    {
        public KomgaSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).IsValidUrl();
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class KomgaSettings : IProviderConfig
    {
        private static readonly KomgaSettingsValidator Validator = new KomgaSettingsValidator();

        public KomgaSettings()
        {
            BaseUrl = "http://localhost:25600";
        }

        [FieldDefinition(0, Label = "Base URL", HelpText = "The base URL of your Komga instance (e.g. http://localhost:25600)")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "Username")]
        public string Username { get; set; }

        [FieldDefinition(2, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password)]
        public string Password { get; set; }

        [FieldDefinition(3, Label = "Update Library", Type = FieldType.Checkbox, HelpText = "Trigger a Komga library scan on import")]
        public bool Notify { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
