using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class ConvertComicFilesCommand : Command
    {
        public List<int> SeriesIds { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;
    }
}
