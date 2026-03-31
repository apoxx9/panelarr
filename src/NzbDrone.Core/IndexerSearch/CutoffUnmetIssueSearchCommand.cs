using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    public class CutoffUnmetIssueSearchCommand : Command
    {
        public int? SeriesId { get; set; }

        public override bool SendUpdatesToClient => true;

        public CutoffUnmetIssueSearchCommand()
        {
        }

        public CutoffUnmetIssueSearchCommand(int seriesId)
        {
            SeriesId = seriesId;
        }
    }
}
