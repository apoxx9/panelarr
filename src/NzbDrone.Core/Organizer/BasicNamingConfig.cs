namespace NzbDrone.Core.Organizer
{
    public class BasicNamingConfig
    {
        public bool IncludeSeriesName { get; set; }
        public bool IncludeIssueTitle { get; set; }
        public bool IncludeQuality { get; set; }
        public bool ReplaceSpaces { get; set; }
        public string Separator { get; set; }
        public string NumberStyle { get; set; }
    }
}
