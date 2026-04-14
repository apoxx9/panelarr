using Equ;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Issues
{
    public class SeriesGroupLink : Entity<SeriesGroupLink>
    {
        public string Position { get; set; }
        public int SeriesPosition { get; set; }
        public int SeriesGroupId { get; set; }
        public int SeriesMetadataId { get; set; }
        public bool IsPrimary { get; set; }

        [MemberwiseEqualityIgnore]
        public LazyLoaded<SeriesGroup> SeriesGroup { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<Issue> Issue { get; set; }

        public override void UseMetadataFrom(SeriesGroupLink other)
        {
            Position = other.Position;
            SeriesPosition = other.SeriesPosition;
            IsPrimary = other.IsPrimary;
        }

        public override void UseDbFieldsFrom(SeriesGroupLink other)
        {
            Id = other.Id;
            SeriesGroupId = other.SeriesGroupId;
            SeriesMetadataId = other.SeriesMetadataId;
        }
    }
}
