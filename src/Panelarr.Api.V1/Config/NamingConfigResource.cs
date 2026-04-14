using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Config
{
    public class NamingConfigResource : RestResource
    {
        public bool RenameComics { get; set; }
        public bool ReplaceIllegalCharacters { get; set; }
        public int ColonReplacementFormat { get; set; }
        public string StandardIssueFormat { get; set; }
        public string AnnualIssueFormat { get; set; }
        public string TPBFormat { get; set; }
        public string SeriesFolderFormat { get; set; }
        public bool IncludeSeriesName { get; set; }
        public bool IncludeIssueTitle { get; set; }
        public bool IncludeQuality { get; set; }
        public bool ReplaceSpaces { get; set; }
        public string Separator { get; set; }
        public string NumberStyle { get; set; }
    }
}
