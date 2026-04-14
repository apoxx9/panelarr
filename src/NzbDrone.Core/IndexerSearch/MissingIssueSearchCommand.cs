using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class MissingIssueSearchCommand : Command
    {
        public int? SeriesId { get; set; }

        public override bool SendUpdatesToClient => true;

        public MissingIssueSearchCommand()
        {
        }

        public MissingIssueSearchCommand(int seriesId)
        {
            SeriesId = seriesId;
        }
    }
}
