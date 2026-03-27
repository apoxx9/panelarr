using System.Collections.Generic;
using NzbDrone.Core.Books;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Extras.Metadata
{
    public interface IMetadata : IProvider
    {
        string GetFilenameAfterMove(Series author, ComicFile comicFile, MetadataFile metadataFile);
        string GetFilenameAfterMove(Series author, string bookPath, MetadataFile metadataFile);
        MetadataFile FindMetadataFile(Series author, string path);
        MetadataFileResult SeriesMetadata(Series author);
        MetadataFileResult IssueMetadata(Series author, ComicFile comicFile);
        List<ImageFileResult> SeriesImages(Series author);
        List<ImageFileResult> IssueImages(Series author, ComicFile comicFile);
    }
}
