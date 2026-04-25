using NzbDrone.Core.Issues;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class ParsedComicInfo
    {
        public string SeriesTitle { get; set; }
        public float? IssueNumber { get; set; }
        public int? VolumeNumber { get; set; }
        public int? Year { get; set; }
        public IssueType IssueType { get; set; }
        public int? TotalIssues { get; set; }
        public ComicFormat Format { get; set; }
        public string ReleaseGroup { get; set; }
        public QualityModel Quality { get; set; }
        public string ReleaseTitle { get; set; }
        public string Source { get; set; }

        public override string ToString()
        {
            if (IssueNumber.HasValue)
            {
                return $"{SeriesTitle} #{IssueNumber:0.##} ({Year})";
            }

            return $"{SeriesTitle} [{IssueType}] ({Year})";
        }
    }
}
