using NzbDrone.Core.Organizer;

namespace Panelarr.Api.V1.Config
{
    public class NamingExampleResource
    {
        public string SingleBookExample { get; set; }
        public string MultiPartBookExample { get; set; }
        public string SeriesFolderExample { get; set; }
    }

    public static class NamingConfigResourceMapper
    {
        public static NamingConfigResource ToResource(this NamingConfig model)
        {
            return new NamingConfigResource
            {
                Id = model.Id,

                RenameBooks = model.RenameBooks,
                ReplaceIllegalCharacters = model.ReplaceIllegalCharacters,
                ColonReplacementFormat = (int)model.ColonReplacementFormat,
                StandardBookFormat = model.StandardBookFormat,
                AnnualIssueFormat = model.AnnualIssueFormat,
                TPBFormat = model.TPBFormat,
                SeriesFolderFormat = model.SeriesFolderFormat
            };
        }

        public static void AddToResource(this BasicNamingConfig basicNamingConfig, NamingConfigResource resource)
        {
            resource.IncludeSeriesName = basicNamingConfig.IncludeSeriesName;
            resource.IncludeBookTitle = basicNamingConfig.IncludeBookTitle;
            resource.IncludeQuality = basicNamingConfig.IncludeQuality;
            resource.ReplaceSpaces = basicNamingConfig.ReplaceSpaces;
            resource.Separator = basicNamingConfig.Separator;
            resource.NumberStyle = basicNamingConfig.NumberStyle;
        }

        public static NamingConfig ToModel(this NamingConfigResource resource)
        {
            return new NamingConfig
            {
                Id = resource.Id,

                RenameBooks = resource.RenameBooks,
                ReplaceIllegalCharacters = resource.ReplaceIllegalCharacters,
                ColonReplacementFormat = (ColonReplacementFormat)resource.ColonReplacementFormat,
                StandardBookFormat = resource.StandardBookFormat,
                AnnualIssueFormat = resource.AnnualIssueFormat,
                TPBFormat = resource.TPBFormat,
                SeriesFolderFormat = resource.SeriesFolderFormat,
            };
        }
    }
}
