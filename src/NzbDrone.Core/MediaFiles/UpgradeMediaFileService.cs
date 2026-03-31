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
        ComicFileMoveResult UpgradeComicFile(ComicFile comicFile, LocalIssue localIssue, bool copyOnly = false);
    }

    public class UpgradeMediaFileService : IUpgradeMediaFiles
    {
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IMoveComicFiles _comicFileMover;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderService _rootFolderService;
        private readonly Logger _logger;

        public UpgradeMediaFileService(IRecycleBinProvider recycleBinProvider,
                                       IMediaFileService mediaFileService,
                                       IMetadataTagService metadataTagService,
                                       IMoveComicFiles comicFileMover,
                                       IDiskProvider diskProvider,
                                       IRootFolderService rootFolderService,
                                       Logger logger)
        {
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _metadataTagService = metadataTagService;
            _comicFileMover = comicFileMover;
            _diskProvider = diskProvider;
            _rootFolderService = rootFolderService;
            _logger = logger;
        }

        public ComicFileMoveResult UpgradeComicFile(ComicFile comicFile, LocalIssue localIssue, bool copyOnly = false)
        {
            var moveFileResult = new ComicFileMoveResult();
            var existingFiles = localIssue.Issue.ComicFiles.Value;

            var rootFolderPath = _diskProvider.GetParentFolder(localIssue.Series.Path);

            // If there are existing issue files and the root folder is missing, throw, so the old file isn't left behind during the import process.
            if (existingFiles.Any() && !_diskProvider.FolderExists(rootFolderPath))
            {
                throw new RootFolderNotFoundException($"Root folder '{rootFolderPath}' was not found.");
            }

            foreach (var file in existingFiles)
            {
                var comicFilePath = file.Path;
                var subfolder = rootFolderPath.GetRelativePath(_diskProvider.GetParentFolder(comicFilePath));

                if (_diskProvider.FileExists(comicFilePath))
                {
                    _logger.Debug("Removing existing issue file: {0}", file);
                    _recycleBinProvider.DeleteFile(comicFilePath, subfolder);
                }

                moveFileResult.OldFiles.Add(file);
                _mediaFileService.Delete(file, DeleteMediaFileReason.Upgrade);
            }

            if (copyOnly)
            {
                moveFileResult.ComicFile = _comicFileMover.CopyComicFile(comicFile, localIssue);
            }
            else
            {
                moveFileResult.ComicFile = _comicFileMover.MoveComicFile(comicFile, localIssue);
            }

            _metadataTagService.WriteTags(comicFile, true);

            return moveFileResult;
        }
    }
}
