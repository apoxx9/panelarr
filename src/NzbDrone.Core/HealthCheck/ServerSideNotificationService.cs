using System.Collections.Generic;

namespace NzbDrone.Core.HealthCheck
{
    public interface IServerSideNotificationService
    {
        public List<HealthCheck> GetServerChecks();
    }

    // There is no panelarr cloud service to push server-side health
    // notifications; this exists to satisfy the health-check pipeline.
    public class ServerSideNotificationService : IServerSideNotificationService
    {
        public List<HealthCheck> GetServerChecks()
        {
            return new List<HealthCheck>();
        }
    }
}
