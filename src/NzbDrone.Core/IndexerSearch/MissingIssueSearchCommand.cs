using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class MissingBookSearchCommand : Command
    {
        public int? SeriesId { get; set; }

        public override bool SendUpdatesToClient => true;

        public MissingBookSearchCommand()
        {
        }

        public MissingBookSearchCommand(int authorId)
        {
            SeriesId = authorId;
        }
    }
}
