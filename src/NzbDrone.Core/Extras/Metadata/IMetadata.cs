using System.Collections.Generic;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Extras.Metadata
{
    public interface IMetadata : IProvider
    {
        string GetFilenameAfterMove(Series series, ComicFile comicFile, MetadataFile metadataFile);
        string GetFilenameAfterMove(Series series, string issuePath, MetadataFile metadataFile);
        MetadataFile FindMetadataFile(Series series, string path);
        MetadataFileResult SeriesMetadata(Series series);
        MetadataFileResult IssueMetadata(Series series, ComicFile comicFile);
        List<ImageFileResult> SeriesImages(Series series);
        List<ImageFileResult> IssueImages(Series series, ComicFile comicFile);
    }
}
