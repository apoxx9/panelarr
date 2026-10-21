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

        // ComicVine can revise a release date backwards onto a day that is
        // already outside MaxAgeDays, so an issue can become missing-and-
        // released having never been searched at all. Catwoman #90 and
        // Nightwing #141 were both re-dated onto a day 20 days gone and sat
        // missing while one of them was available the whole time. Issues that
        // have never been searched are therefore reconsidered out to this
        // horizon; anything older is a deliberate back-catalogue gap.
        public const int NeverSearchedMaxAgeDays = 90;

        public override bool SendUpdatesToClient => true;
    }
}
