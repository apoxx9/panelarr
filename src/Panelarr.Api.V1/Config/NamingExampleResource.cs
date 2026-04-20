using NzbDrone.Core.Organizer;

namespace Panelarr.Api.V1.Config
{
    public class NamingExampleResource
    {
        public string SingleIssueExample { get; set; }
        public string MultiPartIssueExample { get; set; }
        public string SeriesFolderExample { get; set; }
    }

    public static class NamingConfigResourceMapper
    {
        public static NamingConfigResource ToResource(this NamingConfig model)
        {
            return new NamingConfigResource
            {
                Id = model.Id,

                RenameComics = model.RenameComics,
                ReplaceIllegalCharacters = model.ReplaceIllegalCharacters,
                ColonReplacementFormat = (int)model.ColonReplacementFormat,
                StandardIssueFormat = model.StandardIssueFormat,
                AnnualIssueFormat = model.AnnualIssueFormat,
                TPBFormat = model.TPBFormat,
                SeriesFolderFormat = model.SeriesFolderFormat
            };
        }

        public static void AddToResource(this BasicNamingConfig basicNamingConfig, NamingConfigResource resource)
        {
            resource.IncludeSeriesName = basicNamingConfig.IncludeSeriesName;
            resource.IncludeIssueTitle = basicNamingConfig.IncludeIssueTitle;
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

                RenameComics = resource.RenameComics,
                ReplaceIllegalCharacters = resource.ReplaceIllegalCharacters,
                ColonReplacementFormat = (ColonReplacementFormat)resource.ColonReplacementFormat,
                StandardIssueFormat = resource.StandardIssueFormat,
                AnnualIssueFormat = resource.AnnualIssueFormat,
                TPBFormat = resource.TPBFormat,
                SeriesFolderFormat = resource.SeriesFolderFormat,
            };
        }
    }
}
