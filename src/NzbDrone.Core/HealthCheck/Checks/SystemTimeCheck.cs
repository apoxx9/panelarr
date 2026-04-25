using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.HealthCheck.Checks
{
    public class SystemTimeCheck : HealthCheckBase
    {
        public SystemTimeCheck(IHttpClient client, IPanelarrCloudRequestBuilder cloudRequestBuilder, ILocalizationService localizationService, Logger logger)
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
