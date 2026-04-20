using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class RetagSeriesCommand : Command
    {
        public List<int> SeriesIds { get; set; }
        public bool UpdateCovers { get; set; }
        public bool EmbedMetadata { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;

        public RetagSeriesCommand()
        {
            SeriesIds = new List<int>();
        }
    }
}
