using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Extras.Others
{
    public class OtherExtraService : ExtraFileManager<OtherExtraFile>
    {
        private readonly IOtherExtraFileService _otherExtraFileService;
        private readonly IMediaFileAttributeService _mediaFileAttributeService;

        public OtherExtraService(IConfigService configService,
                                 IDiskProvider diskProvider,
                                 IDiskTransferService diskTransferService,
                                 IOtherExtraFileService otherExtraFileService,
                                 IMediaFileAttributeService mediaFileAttributeService,
                                 Logger logger)
            : base(configService, diskProvider, diskTransferService, logger)
        {
            _otherExtraFileService = otherExtraFileService;
            _mediaFileAttributeService = mediaFileAttributeService;
        }

        public override int Order => 2;

        public override IEnumerable<ExtraFile> CreateAfterSeriesScan(Series author, List<ComicFile> comicFiles)
        {
            return Enumerable.Empty<ExtraFile>();
        }

        public override IEnumerable<ExtraFile> CreateAfterComicImport(Series author, ComicFile comicFile)
        {
            return Enumerable.Empty<ExtraFile>();
        }

        public override IEnumerable<ExtraFile> CreateAfterComicImport(Series author, Issue issue, string authorFolder, string bookFolder)
        {
            return Enumerable.Empty<ExtraFile>();
        }

        public override IEnumerable<ExtraFile> MoveFilesAfterRename(Series author, List<ComicFile> comicFiles)
        {
            var extraFiles = _otherExtraFileService.GetFilesBySeries(author.Id);
            var movedFiles = new List<OtherExtraFile>();

            foreach (var comicFile in comicFiles)
            {
                var extraFilesForTrackFile = extraFiles.Where(m => m.ComicFileId == comicFile.Id).ToList();

                foreach (var extraFile in extraFilesForTrackFile)
                {
                    movedFiles.AddIfNotNull(MoveFile(author, comicFile, extraFile));
                }
            }

            _otherExtraFileService.Upsert(movedFiles);

            return movedFiles;
        }

        public override ExtraFile Import(Series author, ComicFile comicFile, string path, string extension, bool readOnly)
        {
            var extraFile = ImportFile(author, comicFile, path, readOnly, extension, null);

            _mediaFileAttributeService.SetFilePermissions(path);
            _otherExtraFileService.Upsert(extraFile);

            return extraFile;
        }
    }
}
