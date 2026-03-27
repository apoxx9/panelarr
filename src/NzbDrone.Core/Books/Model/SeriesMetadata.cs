using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Books
{
    public class SeriesMetadata : Entity<SeriesMetadata>
    {
        public SeriesMetadata()
        {
            Images = new List<MediaCover.MediaCover>();
            Genres = new List<string>();
            Links = new List<Links>();
            Ratings = new Ratings();
        }

        public string ForeignSeriesId { get; set; }
        public string TitleSlug { get; set; }
        public string Name { get; set; }
        public string SortName { get; set; }
        public string Overview { get; set; }
        public string Disambiguation { get; set; }
        public SeriesStatusType Status { get; set; }
        public SeriesType SeriesType { get; set; }
        public int? Year { get; set; }
        public int? VolumeNumber { get; set; }
        public int? PublisherId { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public List<Links> Links { get; set; }
        public List<string> Genres { get; set; }
        public Ratings Ratings { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}][{1}]", ForeignSeriesId, Name.NullSafe());
        }

        public override void UseMetadataFrom(SeriesMetadata other)
        {
            ForeignSeriesId = other.ForeignSeriesId;
            TitleSlug = other.TitleSlug;
            Name = other.Name;
            SortName = other.SortName;
            Overview = other.Overview.IsNullOrWhiteSpace() ? Overview : other.Overview;
            Disambiguation = other.Disambiguation;
            Status = other.Status;
            SeriesType = other.SeriesType;
            Year = other.Year;
            VolumeNumber = other.VolumeNumber;
            PublisherId = other.PublisherId;
            Images = other.Images.Any() ? other.Images : Images;
            Links = other.Links;
            Genres = other.Genres;
            Ratings = other.Ratings.Votes > 0 ? other.Ratings : Ratings;
        }
    }
}
