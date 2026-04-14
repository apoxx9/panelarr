using NLog;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles
{
    public class ArchiveInspectionService : IHandle<ComicFileAddedEvent>
    {
        private readonly IArchiveInspector _archiveInspector;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public ArchiveInspectionService(
            IArchiveInspector archiveInspector,
            IMediaFileService mediaFileService,
            Logger logger)
        {
            _archiveInspector = archiveInspector;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public void Handle(ComicFileAddedEvent message)
        {
            var comicFile = message.ComicFile;

            if (string.IsNullOrWhiteSpace(comicFile.Path))
            {
                return;
            }

            _logger.Debug("Inspecting archive for {0}", comicFile.Path);

            var result = _archiveInspector.Inspect(comicFile.Path, comicFile.ComicFormat);

            if (result.Error != null)
            {
                _logger.Warn("Archive inspection returned error for {0}: {1}", comicFile.Path, result.Error);
            }

            comicFile.ImageCount = result.ImageCount > 0 ? result.ImageCount : result.PageCount;
            comicFile.ImageQualityScore = result.ImageQualityScore;

            _mediaFileService.Update(comicFile);

            _logger.Debug(
                "Archive inspection complete for {0}: {1} pages, quality score {2:F3}",
                comicFile.Path,
                comicFile.ImageCount,
                comicFile.ImageQualityScore);
        }
    }
}
