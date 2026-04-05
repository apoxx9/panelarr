using System;
using System.Collections.Generic;
using System.IO;
using FluentValidation.Results;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Extras.Metadata
{
    public abstract class MetadataBase<TSettings> : IMetadata
        where TSettings : IProviderConfig, new()
    {
        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }

        public ValidationResult Test()
        {
            return new ValidationResult();
        }

        public virtual string GetFilenameAfterMove(Series series, ComicFile comicFile, MetadataFile metadataFile)
        {
            var existingFilename = Path.Combine(series.Path, metadataFile.RelativePath);
            var extension = Path.GetExtension(existingFilename).TrimStart('.');
            var newFileName = Path.ChangeExtension(comicFile.Path, extension);

            return newFileName;
        }

        public virtual string GetFilenameAfterMove(Series series, string issuePath, MetadataFile metadataFile)
        {
            var existingFilename = Path.GetFileName(metadataFile.RelativePath);
            var newFileName = Path.Combine(series.Path, issuePath, existingFilename);

            return newFileName;
        }

        public abstract MetadataFile FindMetadataFile(Series series, string path);

        public abstract MetadataFileResult SeriesMetadata(Series series);
        public abstract MetadataFileResult IssueMetadata(Series series, ComicFile comicFile);
        public abstract List<ImageFileResult> SeriesImages(Series series);
        public abstract List<ImageFileResult> IssueImages(Series series, ComicFile comicFile);

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }

        protected TSettings Settings => (TSettings)Definition.Settings;

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
