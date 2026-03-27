using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.Books.Commands
{
    public class RefreshSeriesCommand : Command
    {
        public int? SeriesId { get; set; }
        public bool IsNewSeries { get; set; }

        public RefreshSeriesCommand()
        {
        }

        public RefreshSeriesCommand(int? authorId, bool isNewSeries = false)
        {
            SeriesId = authorId;
            IsNewSeries = isNewSeries;
        }

        public override bool SendUpdatesToClient => true;

        public override bool UpdateScheduledTask => !SeriesId.HasValue;

        public override string CompletionMessage => "Completed";
    }
}
