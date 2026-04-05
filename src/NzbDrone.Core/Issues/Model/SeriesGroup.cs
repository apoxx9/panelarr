using System.Collections.Generic;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Issues
{
    public class SeriesGroup : Entity<SeriesGroup>
    {
        public string ForeignSeriesGroupId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SortTitle { get; set; }
        public LazyLoaded<List<SeriesGroupLink>> LinkItems { get; set; }
        public LazyLoaded<List<SeriesMetadata>> SeriesMetadata { get; set; }

        // Alias for ForeignSeriesGroupId for callers that use the old name
        public string ForeignSeriesId => ForeignSeriesGroupId;

        // Stub properties for callers that reference these
        public bool Numbered { get; set; }
        public int WorkCount { get; set; }
        public int PrimaryWorkCount { get; set; }
        public LazyLoaded<List<Issue>> Issues { get; set; }

        public override void UseMetadataFrom(SeriesGroup other)
        {
            ForeignSeriesGroupId = other.ForeignSeriesGroupId;
            Title = other.Title;
            Description = other.Description;
            SortTitle = other.SortTitle;
        }

        public override void UseDbFieldsFrom(SeriesGroup other)
        {
            Id = other.Id;
        }
    }
}
