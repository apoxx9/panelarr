using System.Collections.Generic;

namespace NzbDrone.Core.ReadingLists
{
    // Result of resolving id-less unresolved slots against the metadata
    // provider: name-only slots (e.g. from community CBLs without Database
    // blocks) get their ForeignSeriesId backfilled so the add-missing-series
    // affordance can operate on them.
    public class ProviderResolveReport
    {
        public int SlotsConsidered { get; set; }
        public int Linked { get; set; }
        public List<ProviderResolveAmbiguity> Ambiguous { get; set; } = new List<ProviderResolveAmbiguity>();
        public List<string> NotFound { get; set; } = new List<string>();
    }

    public class ProviderResolveAmbiguity
    {
        public string SeriesName { get; set; }
        public List<int> SlotIds { get; set; } = new List<int>();
        public List<ProviderResolveCandidate> Candidates { get; set; } = new List<ProviderResolveCandidate>();
    }

    public class ProviderResolveCandidate
    {
        public string ForeignSeriesId { get; set; }
        public string Name { get; set; }
        public string Year { get; set; }
        public string Publisher { get; set; }
    }
}
