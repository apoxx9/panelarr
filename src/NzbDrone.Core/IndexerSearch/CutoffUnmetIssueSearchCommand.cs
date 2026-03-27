using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class CutoffUnmetBookSearchCommand : Command
    {
        public int? SeriesId { get; set; }

        public override bool SendUpdatesToClient => true;

        public CutoffUnmetBookSearchCommand()
        {
        }

        public CutoffUnmetBookSearchCommand(int authorId)
        {
            SeriesId = authorId;
        }
    }
}
