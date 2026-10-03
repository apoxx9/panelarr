using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    // The RSS safety net: a flooded feed window silently drops releases and
    // nothing else searches automatically. Only RECENTLY released missing
    // issues are re-searched - old gaps are deliberate, not feed accidents.
    public class RecentMissingIssueSearchCommand : Command
    {
        public const int MaxAgeDays = 14;
        public const int MaxIssues = 30;

        public override bool SendUpdatesToClient => true;
    }
}
