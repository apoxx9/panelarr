using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Extras.Files
{
    public interface IManageExtraFiles
    {
        int Order { get; }
        IEnumerable<ExtraFile> CreateAfterSeriesScan(Series series, List<ComicFile> comicFiles);
        IEnumerable<ExtraFile> CreateAfterComicImport(Series series, ComicFile comicFile);
        IEnumerable<ExtraFile> CreateAfterComicImport(Series series, Issue issue, string seriesFolder, string issueFolder);
        IEnumerable<ExtraFile> MoveFilesAfterRename(Series series, List<ComicFile> comicFiles);
        ExtraFile Import(Series series, ComicFile comicFile, string path, string extension, bool readOnly);
    }

    public abstract class ExtraFileManager<TExtraFile> : IManageExtraFiles
        where TExtraFile : ExtraFile, new()
    {
        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly IDiskTransferService _diskTransferService;
        private readonly Logger _logger;

        public ExtraFileManager(IConfigService configService,
                                IDiskProvider diskProvider,
                                IDiskTransferService diskTransferService,
                                Logger logger)
        {
            _configService = configService;
            _diskProvider = diskProvider;
            _diskTransferService = diskTransferService;
            _logger = logger;
        }

        public abstract int Order { get; }
        public abstract IEnumerable<ExtraFile> CreateAfterSeriesScan(Series series, List<ComicFile> comicFiles);
        public abstract IEnumerable<ExtraFile> CreateAfterComicImport(Series series, ComicFile comicFile);
        public abstract IEnumerable<ExtraFile> CreateAfterComicImport(Series series, Issue issue, string seriesFolder, string issueFolder);
        public abstract IEnumerable<ExtraFile> MoveFilesAfterRename(Series series, List<ComicFile> comicFiles);
        public abstract ExtraFile Import(Series series, ComicFile comicFile, string path, string extension, bool readOnly);

        protected TExtraFile ImportFile(Series series, ComicFile comicFile, string path, bool readOnly, string extension, string fileNameSuffix = null)
        {
            var newFolder = Path.GetDirectoryName(comicFile.Path);
            var filenameBuilder = new StringBuilder(Path.GetFileNameWithoutExtension(comicFile.Path));

            if (fileNameSuffix.IsNotNullOrWhiteSpace())
            {
                filenameBuilder.Append(fileNameSuffix);
            }

            filenameBuilder.Append(extension);

            var newFileName = Path.Combine(newFolder, filenameBuilder.ToString());
            var transferMode = TransferMode.Move;

            if (readOnly)
            {
                transferMode = _configService.CopyUsingHardlinks ? TransferMode.HardLinkOrCopy : TransferMode.Copy;
            }

            _diskTransferService.TransferFile(path, newFileName, transferMode, true);

            return new TExtraFile
            {
                SeriesId = series.Id,
                IssueId = comicFile.IssueId,
                ComicFileId = comicFile.Id,
                RelativePath = series.Path.GetRelativePath(newFileName),
                Extension = extension
            };
        }

        protected TExtraFile MoveFile(Series series, ComicFile comicFile, TExtraFile extraFile, string fileNameSuffix = null)
        {
            _logger.Trace("Renaming extra file: {0}", extraFile);

            var newFolder = Path.GetDirectoryName(comicFile.Path);
            var filenameBuilder = new StringBuilder(Path.GetFileNameWithoutExtension(comicFile.Path));

            if (fileNameSuffix.IsNotNullOrWhiteSpace())
            {
                filenameBuilder.Append(fileNameSuffix);
            }

            filenameBuilder.Append(extraFile.Extension);

            var existingFileName = Path.Combine(series.Path, extraFile.RelativePath);
            var newFileName = Path.Combine(newFolder, filenameBuilder.ToString());

            if (newFileName.PathNotEquals(existingFileName))
            {
                try
                {
                    _logger.Trace("Renaming extra file: {0} to {1}", extraFile, newFileName);

                    _diskProvider.MoveFile(existingFileName, newFileName);
                    extraFile.RelativePath = series.Path.GetRelativePath(newFileName);

                    _logger.Trace("Renamed extra file from: {0}", extraFile);

                    return extraFile;
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to move file after rename: {0}", existingFileName);
                }
            }

            return null;
        }
    }
}
