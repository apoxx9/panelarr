using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Books.Commands
{
    public class BulkRefreshSeriesCommand : Command
    {
        public BulkRefreshSeriesCommand()
        {
        }

        public BulkRefreshSeriesCommand(List<int> authorIds, bool areNewSeriess = false)
        {
            SeriesIds = authorIds;
            AreNewSeriess = areNewSeriess;
        }

        public List<int> SeriesIds { get; set; }
        public bool AreNewSeriess { get; set; }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => false;
    }
}
