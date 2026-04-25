namespace NzbDrone.Core.MetadataSource
{
    public class MetadataSearchCriteria
    {
        public string Term { get; set; }
        public int? Year { get; set; }

        public MetadataSearchCriteria(string term, int? year = null)
        {
            Term = term?.Trim() ?? string.Empty;
            Year = year;
        }
    }
}
