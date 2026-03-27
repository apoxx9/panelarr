using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Books.Commands
{
    public class BulkRefreshSeriesCommand : Command
    {
        public BulkRefreshSeriesCommand()
        {
        }

        public BulkRefreshSeriesCommand(List<int> authorIds, bool areNewSeries = false)
        {
            SeriesIds = authorIds;
            AreNewSeries = areNewSeries;
        }

        public List<int> SeriesIds { get; set; }
        public bool AreNewSeries { get; set; }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => false;
    }
}
