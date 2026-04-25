using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Issues.Commands
{
    public class BulkRefreshSeriesCommand : Command
    {
        public BulkRefreshSeriesCommand()
        {
        }

        public BulkRefreshSeriesCommand(List<int> seriesIds, bool areNewSeries = false)
        {
            SeriesIds = seriesIds;
            AreNewSeries = areNewSeries;
        }

        public List<int> SeriesIds { get; set; }
        public bool AreNewSeries { get; set; }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => false;
    }
}
