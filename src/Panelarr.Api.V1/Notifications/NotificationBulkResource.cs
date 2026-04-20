using NzbDrone.Core.Notifications;

namespace Panelarr.Api.V1.Notifications
{
    public class NotificationBulkResource : ProviderBulkResource<NotificationBulkResource>
    {
    }

    public class NotificationBulkResourceMapper : ProviderBulkResourceMapper<NotificationBulkResource, NotificationDefinition>
    {
    }
}
