using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Issues.Relations
{
    // Directional and typed so future features (auto-suggest, index grouping,
    // reading order) have the information they need, but the v1 UI renders
    // links symmetrically and treats a pair as linked regardless of direction.
    public class SeriesRelation : ModelBase
    {
        public int SeriesId { get; set; }
        public int RelatedSeriesId { get; set; }
        public SeriesRelationType RelationType { get; set; }
    }

    public enum SeriesRelationType
    {
        Related = 0,
        Annual = 1,
        SpinOff = 2
    }
}
