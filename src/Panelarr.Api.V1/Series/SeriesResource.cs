using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaCover;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Series
{
    public class SeriesResource : RestResource
    {
        //Todo: Sorters should be done completely on the client
        //Todo: Is there an easy way to keep IgnoreArticlesWhenSorting in sync between, SeriesGroup, History, Missing?
        //Todo: We should get the entire Profile instead of ID and Name separately
        [JsonIgnore]
        public int SeriesMetadataId { get; set; }
        public SeriesStatusType Status { get; set; }

        public bool Ended => Status == SeriesStatusType.Ended;

        public string SeriesName { get; set; }
        public string SeriesNameLastFirst { get; set; }
        public string ForeignSeriesId { get; set; }
        public string TitleSlug { get; set; }
        public string Overview { get; set; }
        public string Disambiguation { get; set; }
        public List<Links> Links { get; set; }

        public Issue NextBook { get; set; }
        public Issue LastBook { get; set; }

        public List<MediaCover> Images { get; set; }

        public string RemotePoster { get; set; }

        //View & Edit
        public string Path { get; set; }
        public int QualityProfileId { get; set; }

        //Editing Only
        public bool Monitored { get; set; }
        public NewItemMonitorTypes MonitorNewItems { get; set; }

        public string RootFolderPath { get; set; }
        public string Folder { get; set; }
        public List<string> Genres { get; set; }
        public string CleanName { get; set; }
        public string SortName { get; set; }
        public HashSet<int> Tags { get; set; }
        public DateTime Added { get; set; }
        public AddSeriesOptions AddOptions { get; set; }
        public Ratings Ratings { get; set; }

        public SeriesStatisticsResource Statistics { get; set; }
    }

    public static class SeriesResourceMapper
    {
        public static SeriesResource ToResource(this NzbDrone.Core.Books.Series model)
        {
            if (model == null)
            {
                return null;
            }

            return new SeriesResource
            {
                Id = model.Id,
                SeriesMetadataId = model.SeriesMetadataId,

                SeriesName = model.Name,
                SeriesNameLastFirst = model.Metadata.Value.SortName,

                //AlternateTitles
                SortName = model.Metadata.Value.SortName,

                Status = model.Metadata.Value.Status,
                Overview = model.Metadata.Value.Overview,
                Disambiguation = model.Metadata.Value.Disambiguation,

                Images = model.Metadata.Value.Images.JsonClone(),

                Path = model.Path,
                QualityProfileId = model.QualityProfileId,
                Links = model.Metadata.Value.Links,

                Monitored = model.Monitored,
                MonitorNewItems = model.MonitorNewItems,

                CleanName = model.CleanName,
                ForeignSeriesId = model.Metadata.Value.ForeignSeriesId,
                TitleSlug = model.Metadata.Value.TitleSlug,

                // Root folder path is now calculated from the author path
                // RootFolderPath = model.RootFolderPath,
                Genres = model.Metadata.Value.Genres,
                Tags = model.Tags,
                Added = model.Added,
                AddOptions = model.AddOptions,
                Ratings = model.Metadata.Value.Ratings,

                Statistics = new SeriesStatisticsResource()
            };
        }

        public static NzbDrone.Core.Books.Series ToModel(this SeriesResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new NzbDrone.Core.Books.Series
            {
                Id = resource.Id,

                Metadata = new NzbDrone.Core.Books.SeriesMetadata
                {
                    ForeignSeriesId = resource.ForeignSeriesId,
                    TitleSlug = resource.TitleSlug,
                    Name = resource.SeriesName,
                    SortName = resource.SortName,
                    Status = resource.Status,
                    Overview = resource.Overview,
                    Links = resource.Links,
                    Images = resource.Images,
                    Genres = resource.Genres,
                    Ratings = resource.Ratings,
                },

                //AlternateTitles
                Path = resource.Path,
                QualityProfileId = resource.QualityProfileId,

                Monitored = resource.Monitored,
                MonitorNewItems = resource.MonitorNewItems,

                CleanName = resource.CleanName,
                RootFolderPath = resource.RootFolderPath,

                Tags = resource.Tags,
                Added = resource.Added,
                AddOptions = resource.AddOptions
            };
        }

        public static NzbDrone.Core.Books.Series ToModel(this SeriesResource resource, NzbDrone.Core.Books.Series author)
        {
            var updatedSeries = resource.ToModel();

            author.ApplyChanges(updatedSeries);

            return author;
        }

        public static List<SeriesResource> ToResource(this IEnumerable<NzbDrone.Core.Books.Series> author)
        {
            return author.Select(ToResource).ToList();
        }

        public static List<NzbDrone.Core.Books.Series> ToModel(this IEnumerable<SeriesResource> resources)
        {
            return resources.Select(ToModel).ToList();
        }
    }
}
