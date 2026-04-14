using System.Collections.Generic;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Issues.Events
{
    public class SeriesImportedEvent : IEvent
    {
        public List<int> SeriesIds { get; private set; }
        public bool DoRefresh { get; private set; }

        public SeriesImportedEvent(List<int> seriesIds, bool doRefresh = true)
        {
            SeriesIds = seriesIds;
            DoRefresh = doRefresh;
        }
    }
}
