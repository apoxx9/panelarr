using System.Collections.Generic;

namespace NzbDrone.Core.Issues
{
    public class Publisher : Entity<Publisher>
    {
        public Publisher()
        {
            Images = new List<MediaCover.MediaCover>();
        }

        public string ForeignPublisherId { get; set; }
        public string Name { get; set; }
        public string CleanName { get; set; }
        public string Description { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }

        public override void UseMetadataFrom(Publisher other)
        {
            ForeignPublisherId = other.ForeignPublisherId;
            Name = other.Name;
            CleanName = other.CleanName;
            Description = other.Description;
            Images = other.Images;
        }

        public override void UseDbFieldsFrom(Publisher other)
        {
            Id = other.Id;
        }
    }
}
