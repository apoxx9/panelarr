using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class ParsedFileTagInfo
    {
        public string Title { get; set; }
        public string CleanTitle { get; set; }
        public List<string> Series { get; set; }
        public string IssueTitle { get; set; }
        public string SeriesTitle { get; set; }
        public string SeriesIndex { get; set; }
        public string ForeignIssueId { get; set; }
        public int DiscNumber { get; set; }
        public uint Year { get; set; }
        public string Publisher { get; set; }
        public string Disambiguation { get; set; }
        public QualityModel Quality { get; set; }
        public int[] PartNumbers { get; set; }
        public string ReleaseGroup { get; set; }
        public string ReleaseHash { get; set; }

        public ParsedFileTagInfo()
        {
            Series = new List<string>();
            PartNumbers = new int[0];
        }

        public override string ToString()
        {
            var trackString = "[Unknown Track]";

            if (PartNumbers != null && PartNumbers.Any())
            {
                trackString = string.Format("{0}", string.Join("-", PartNumbers.Select(c => c.ToString("00"))));
            }

            return string.Format("{0} - {1} - {2}:{3} {4}: {5}", Series.ConcatToString(" & "), IssueTitle, DiscNumber, trackString, Title, Quality);
        }
    }
}
