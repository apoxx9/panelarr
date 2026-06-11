using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.CustomFormats
{
    public class CustomFormatInput
    {
        public ParsedIssueInfo IssueInfo { get; set; }
        public Series Series { get; set; }
        public long Size { get; set; }
        public IndexerFlags IndexerFlags { get; set; }
        public string Filename { get; set; }

        // Comic-specific fields, populated when evaluating an existing ComicFile
        // whose archive has been inspected; null = unknown (remote releases,
        // uninspected files) and comic conditions will not match.
        public float? ImageQualityScore { get; set; }
        public int? ImageCount { get; set; }
    }
}
