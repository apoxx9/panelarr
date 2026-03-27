using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IUpgradeMediaFiles
    {
        ComicFileMoveResult UpgradeBookFile(ComicFile comicFile, LocalBook localBook, bool copyOnly = false);
    }

    public class UpgradeMediaFileService : IUpgradeMediaFiles
    {
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IMoveBookFiles _bookFileMover;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderService _rootFolderService;
        private readonly Logger _logger;

        public UpgradeMediaFileService(IRecycleBinProvider recycleBinProvider,
                                       IMediaFileService mediaFileService,
                                       IMetadataTagService metadataTagService,
                                       IMoveBookFiles bookFileMover,
                                       IDiskProvider diskProvider,
                                       IRootFolderService rootFolderService,
                                       Logger logger)
        {
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _metadataTagService = metadataTagService;
            _bookFileMover = bookFileMover;
            _diskProvider = diskProvider;
            _rootFolderService = rootFolderService;
            _logger = logger;
        }

        public ComicFileMoveResult UpgradeBookFile(ComicFile comicFile, LocalBook localBook, bool copyOnly = false)
        {
            var moveFileResult = new ComicFileMoveResult();
            var existingFiles = localBook.Issue.ComicFiles.Value;

            var rootFolderPath = _diskProvider.GetParentFolder(localBook.Series.Path);

            // If there are existing issue files and the root folder is missing, throw, so the old file isn't left behind during the import process.
            if (existingFiles.Any() && !_diskProvider.FolderExists(rootFolderPath))
            {
                throw new RootFolderNotFoundException($"Root folder '{rootFolderPath}' was not found.");
            }

            foreach (var file in existingFiles)
            {
                var bookFilePath = file.Path;
                var subfolder = rootFolderPath.GetRelativePath(_diskProvider.GetParentFolder(bookFilePath));

                if (_diskProvider.FileExists(bookFilePath))
                {
                    _logger.Debug("Removing existing issue file: {0}", file);
                    _recycleBinProvider.DeleteFile(bookFilePath, subfolder);
                }

                moveFileResult.OldFiles.Add(file);
                _mediaFileService.Delete(file, DeleteMediaFileReason.Upgrade);
            }

            if (copyOnly)
            {
                moveFileResult.ComicFile = _bookFileMover.CopyBookFile(comicFile, localBook);
            }
            else
            {
                moveFileResult.ComicFile = _bookFileMover.MoveBookFile(comicFile, localBook);
            }

            _metadataTagService.WriteTags(comicFile, true);

            return moveFileResult;
        }
    }
}
