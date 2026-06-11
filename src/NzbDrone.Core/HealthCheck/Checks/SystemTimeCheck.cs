using NLog;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.HealthCheck.Checks
{
    public class SystemTimeCheck : HealthCheckBase
    {
        public SystemTimeCheck(ILocalizationService localizationService, Logger logger)
            : base(localizationService)
        {
        }

        public override HealthCheck Check()
        {
            // Panelarr does not have a cloud time service — always pass
            return new HealthCheck(GetType());
        }
    }
}
