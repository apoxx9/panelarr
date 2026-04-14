using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Issues
{
    public class SeriesEditedService : IHandle<SeriesEditedEvent>
    {
        public void Handle(SeriesEditedEvent message)
        {
            // MetadataProfileId has been removed from Series; nothing to check here
        }
    }
}
