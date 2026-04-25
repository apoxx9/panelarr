using System;
using System.Linq;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.ComicFiles
{
    public class ComicFileResource : RestResource
    {
        public int SeriesId { get; set; }
        public int IssueId { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime DateAdded { get; set; }
        public QualityModel Quality { get; set; }
        public int QualityWeight { get; set; }
        public int? IndexerFlags { get; set; }
        public MediaInfoResource MediaInfo { get; set; }

        public bool QualityCutoffNotMet { get; set; }
        public ParsedTrackInfo AudioTags { get; set; }
    }

    public static class ComicFileResourceMapper
    {
        private static int QualityWeight(QualityModel quality)
        {
            if (quality == null)
            {
                return 0;
            }

            var qualityWeight = Quality.DefaultQualityDefinitions.Single(q => q.Quality == quality.Quality).Weight;
            qualityWeight += quality.Revision.Real * 10;
            qualityWeight += quality.Revision.Version;
            return qualityWeight;
        }

        public static ComicFileResource ToResource(this ComicFile model)
        {
            if (model == null)
            {
                return null;
            }

            return new ComicFileResource
            {
                Id = model.Id,
                SeriesId = model.Series?.Value?.Id ?? 0,
                IssueId = model.IssueId,
                Path = model.Path,
                Size = model.Size,
                DateAdded = model.DateAdded,
                Quality = model.Quality,
                QualityWeight = QualityWeight(model.Quality),
                MediaInfo = model.MediaInfo.ToResource()
            };
        }

        public static ComicFileResource ToResource(this ComicFile model, NzbDrone.Core.Issues.Series series, IUpgradableSpecification upgradableSpecification)
        {
            if (model == null)
            {
                return null;
            }

            return new ComicFileResource
            {
                Id = model.Id,

                SeriesId = series.Id,
                IssueId = model.IssueId,
                Path = model.Path,
                Size = model.Size,
                DateAdded = model.DateAdded,
                Quality = model.Quality,
                QualityWeight = QualityWeight(model.Quality),
                MediaInfo = model.MediaInfo.ToResource(),
                QualityCutoffNotMet = upgradableSpecification.QualityCutoffNotMet(series.QualityProfile.Value, model.Quality),
                IndexerFlags = (int)model.IndexerFlags
            };
        }
    }
}
