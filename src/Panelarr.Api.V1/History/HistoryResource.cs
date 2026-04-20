using System;
using System.Collections.Generic;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.History;
using NzbDrone.Core.Qualities;
using Panelarr.Api.V1.CustomFormats;
using Panelarr.Api.V1.Issues;
using Panelarr.Api.V1.Series;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.History
{
    public class HistoryResource : RestResource
    {
        public int IssueId { get; set; }
        public int SeriesId { get; set; }
        public string SourceTitle { get; set; }
        public QualityModel Quality { get; set; }
        public List<CustomFormatResource> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public bool QualityCutoffNotMet { get; set; }
        public DateTime Date { get; set; }
        public string DownloadId { get; set; }

        public EntityHistoryEventType EventType { get; set; }

        public Dictionary<string, string> Data { get; set; }

        public IssueResource Issue { get; set; }
        public SeriesResource Series { get; set; }
    }

    public static class HistoryResourceMapper
    {
        public static HistoryResource ToResource(this EntityHistory model, ICustomFormatCalculationService formatCalculator)
        {
            if (model == null)
            {
                return null;
            }

            var customFormats = formatCalculator.ParseCustomFormat(model, model.Series);
            var customFormatScore = model.Series?.QualityProfile?.Value?.CalculateCustomFormatScore(customFormats) ?? 0;

            return new HistoryResource
            {
                Id = model.Id,

                IssueId = model.IssueId,
                SeriesId = model.SeriesId,
                SourceTitle = model.SourceTitle,
                Quality = model.Quality,
                CustomFormats = customFormats.ToResource(false),
                CustomFormatScore = customFormatScore,

                //QualityCutoffNotMet
                Date = model.Date,
                DownloadId = model.DownloadId,

                EventType = model.EventType,

                Data = model.Data

                //Episode
                //SeriesGroup
            };
        }
    }
}
