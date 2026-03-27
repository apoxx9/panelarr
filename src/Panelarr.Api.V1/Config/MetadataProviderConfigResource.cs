using NzbDrone.Core.Configuration;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Config
{
    public class MetadataProviderConfigResource : RestResource
    {
        public WriteAudioTagsType WriteAudioTags { get; set; }
        public bool ScrubAudioTags { get; set; }
        public WriteIssueTagsType WriteIssueTags { get; set; }
        public bool UpdateCovers { get; set; }
        public bool EmbedMetadata { get; set; }

        // Metadata provider credentials
        public string MetronUsername { get; set; }
        public string MetronPassword { get; set; }
        public string ComicVineApiKey { get; set; }
    }

    public static class MetadataProviderConfigResourceMapper
    {
        public static MetadataProviderConfigResource ToResource(IConfigService model)
        {
            return new MetadataProviderConfigResource
            {
                WriteAudioTags = model.WriteAudioTags,
                ScrubAudioTags = model.ScrubAudioTags,
                WriteIssueTags = model.WriteIssueTags,
                UpdateCovers = model.UpdateCovers,
                EmbedMetadata = model.EmbedMetadata,
                MetronUsername = model.MetronUsername,
                MetronPassword = model.MetronPassword,
                ComicVineApiKey = model.ComicVineApiKey
            };
        }
    }
}
