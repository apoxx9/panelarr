using NzbDrone.Core.Configuration;
using NzbDrone.Core.Configuration.Events;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ConfigSavedEvent))]
    public class MetadataProviderCheck : HealthCheckBase
    {
        private readonly IConfigService _configService;

        public MetadataProviderCheck(IConfigService configService, ILocalizationService localizationService)
            : base(localizationService)
        {
            _configService = configService;
        }

        public override HealthCheck Check()
        {
            var hasComicVine = !string.IsNullOrWhiteSpace(_configService.ComicVineApiKey);
            var hasMetron = !string.IsNullOrWhiteSpace(_configService.MetronUsername);

            if (!hasComicVine && !hasMetron)
            {
                return new HealthCheck(GetType(), HealthCheckResult.Warning, _localizationService.GetLocalizedString("MetadataProviderHealthCheckMessage"), "#no-metadata-providers-configured");
            }

            return new HealthCheck(GetType());
        }
    }
}
