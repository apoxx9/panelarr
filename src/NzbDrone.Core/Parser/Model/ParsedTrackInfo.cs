using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class ParsedTrackInfo
    {
        //public int TrackNumber { get; set; }
        public string Title { get; set; }
        public string CleanTitle { get; set; }
        public List<string> Series { get; set; }
        public string IssueTitle { get; set; }
        public string SeriesTitle { get; set; }
        public string SeriesIndex { get; set; }
        public string Isbn { get; set; }
        public string Asin { get; set; }
        public string ForeignIssueId { get; set; }
        public string SeriesMBId { get; set; }
        public string IssueMBId { get; set; }
        public string ReleaseMBId { get; set; }
        public string RecordingMBId { get; set; }
        public string TrackMBId { get; set; }
        public int DiscNumber { get; set; }
        public int DiscCount { get; set; }
        public IsoCountry Country { get; set; }
        public uint Year { get; set; }
        public string Publisher { get; set; }
        public string Label { get; set; }
        public string Source { get; set; }
        public string CatalogNumber { get; set; }
        public string Disambiguation { get; set; }
        public TimeSpan Duration { get; set; }
        public QualityModel Quality { get; set; }
        public MediaInfoModel MediaInfo { get; set; }
        public int[] TrackNumbers { get; set; }
        public string Language { get; set; }
        public string ReleaseGroup { get; set; }
        public string ReleaseHash { get; set; }

        public ParsedTrackInfo()
        {
            Series = new List<string>();
            TrackNumbers = new int[0];
        }

        public override string ToString()
        {
            var trackString = "[Unknown Track]";

            if (TrackNumbers != null && TrackNumbers.Any())
            {
                trackString = string.Format("{0}", string.Join("-", TrackNumbers.Select(c => c.ToString("00"))));
            }

            return string.Format("{0} - {1} - {2}:{3} {4}: {5}", Series.ConcatToString(" & "), IssueTitle, DiscNumber, trackString, Title, Quality);
        }
    }
}
