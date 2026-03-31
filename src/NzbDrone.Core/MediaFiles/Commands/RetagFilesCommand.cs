using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.Commands
{
    public class RetagFilesCommand : Command
    {
        public int SeriesId { get; set; }
        public List<int> Files { get; set; }
        public bool UpdateCovers { get; set; }
        public bool EmbedMetadata { get; set; }

        public override bool SendUpdatesToClient => true;
        public override bool RequiresDiskAccess => true;

        public RetagFilesCommand()
        {
        }

        public RetagFilesCommand(int seriesId, List<int> files)
        {
            SeriesId = seriesId;
            Files = files;
        }
    }
}
