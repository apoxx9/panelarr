using System.Collections.Generic;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class ParsedComicInfo
    {
        public string SeriesTitle { get; set; }

        // "Annual"/"Special" and a year matching the release year are stripped
        // from SeriesTitle, because for most releases they are metadata about
        // an issue rather than part of the series name. But ComicVine also
        // publishes annuals as their own volumes, literally named
        // "Absolute Batman 2025 Annual" - for those, stripping both tokens
        // deletes the identity and collapses the title onto the parent series.
        // The parser cannot tell the two apart; only the library can. So every
        // reading of the name is offered here, most specific first, and
        // resolution picks the first one that names a series that exists.
        public List<string> SeriesTitleCandidates { get; set; }
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
        public bool IsMultiIssue { get; set; }

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
