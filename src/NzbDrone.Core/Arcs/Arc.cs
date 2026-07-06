using System;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Arcs
{
    // A user-curated, ordered list of issues spanning series — story arc,
    // event, creator run, or custom reading order. The model is deliberately
    // the CBL reading-list shape enriched with our ids: a lossless CBL
    // round-trip is an acceptance criterion (docs/story-arcs.md).
    public class Arc : ModelBase
    {
        public string Name { get; set; }

        // cv:<story_arc id> when created from the provider; null for
        // CBL-imported or manual arcs.
        public string ForeignArcId { get; set; }

        public ArcType Type { get; set; }
        public string Publisher { get; set; }
        public string Description { get; set; }
        public DateTime Added { get; set; }
    }

    public enum ArcType
    {
        Arc = 0,
        Event = 1,
        ReadingOrder = 2,
        Custom = 3
    }
}
