using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Issues
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

        // Override tracking
        public bool IsOverridden { get; set; }
        public string OverriddenFields { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}][{1}]", ForeignSeriesId, Name.NullSafe());
        }

        public override void UseMetadataFrom(SeriesMetadata other)
        {
            var overridden = IsOverridden
                ? (OverriddenFields ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();

            if (!IsProtectedField(overridden, "ForeignSeriesId"))
            {
                ForeignSeriesId = other.ForeignSeriesId;
            }

            if (!IsProtectedField(overridden, "TitleSlug"))
            {
                TitleSlug = other.TitleSlug;
            }

            if (!IsProtectedField(overridden, "Name"))
            {
                Name = other.Name;
            }

            if (!IsProtectedField(overridden, "SortName"))
            {
                SortName = other.SortName;
            }

            if (!IsProtectedField(overridden, "Overview"))
            {
                Overview = other.Overview.IsNullOrWhiteSpace() ? Overview : other.Overview;
            }

            if (!IsProtectedField(overridden, "Disambiguation"))
            {
                Disambiguation = other.Disambiguation;
            }

            if (!IsProtectedField(overridden, "Status"))
            {
                Status = other.Status;
            }

            if (!IsProtectedField(overridden, "SeriesType"))
            {
                SeriesType = other.SeriesType;
            }

            if (!IsProtectedField(overridden, "Year"))
            {
                Year = other.Year;
            }

            if (!IsProtectedField(overridden, "VolumeNumber"))
            {
                VolumeNumber = other.VolumeNumber;
            }

            if (!IsProtectedField(overridden, "PublisherId"))
            {
                PublisherId = other.PublisherId;
            }

            if (!IsProtectedField(overridden, "Images"))
            {
                Images = other.Images.Any() ? other.Images : Images;
            }

            if (!IsProtectedField(overridden, "Links"))
            {
                Links = other.Links;
            }

            if (!IsProtectedField(overridden, "Genres"))
            {
                Genres = other.Genres;
            }

            if (!IsProtectedField(overridden, "Ratings"))
            {
                Ratings = other.Ratings.Votes > 0 ? other.Ratings : Ratings;
            }
        }

        private bool IsProtectedField(string[] overridden, string field)
        {
            return IsOverridden && Array.IndexOf(overridden, field) >= 0;
        }
    }
}
