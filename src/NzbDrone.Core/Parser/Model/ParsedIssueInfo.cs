using System.Collections.Generic;
using System.Text.Json.Serialization;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class ParsedIssueInfo
    {
        public string IssueTitle { get; set; }
        public string SeriesName { get; set; }
        public SeriesTitleInfo SeriesTitleInfo { get; set; }
        public QualityModel Quality { get; set; }
        public string ReleaseDate { get; set; }
        public bool Discography { get; set; }
        public int DiscographyStart { get; set; }
        public int DiscographyEnd { get; set; }
        public string ReleaseGroup { get; set; }
        public string ReleaseHash { get; set; }
        public string ReleaseVersion { get; set; }
        public string ReleaseTitle { get; set; }

        [JsonIgnore]
        public Dictionary<string, object> ExtraInfo { get; set; } = new Dictionary<string, object>();

        public override string ToString()
        {
            var issueString = "[Unknown Issue]";

            if (IssueTitle != null)
            {
                issueString = string.Format("{0}", IssueTitle);
            }

            return string.Format("{0} - {1} {2}", SeriesName, issueString, Quality);
        }
    }
}
