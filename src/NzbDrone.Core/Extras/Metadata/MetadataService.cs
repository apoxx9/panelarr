using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Extras.Metadata
{
    public class MetadataService : ExtraFileManager<MetadataFile>
    {
        private readonly IMetadataFactory _metadataFactory;
        private readonly ICleanMetadataService _cleanMetadataService;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IOtherExtraFileRenamer _otherExtraFileRenamer;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IHttpClient _httpClient;
        private readonly IMediaFileAttributeService _mediaFileAttributeService;
        private readonly IMetadataFileService _metadataFileService;
        private readonly IIssueService _issueService;
        private readonly Logger _logger;

        public MetadataService(IConfigService configService,
                               IDiskProvider diskProvider,
                               IDiskTransferService diskTransferService,
                               IRecycleBinProvider recycleBinProvider,
                               IOtherExtraFileRenamer otherExtraFileRenamer,
                               IMetadataFactory metadataFactory,
                               ICleanMetadataService cleanMetadataService,
                               IHttpClient httpClient,
                               IMediaFileAttributeService mediaFileAttributeService,
                               IMetadataFileService metadataFileService,
                               IIssueService issueService,
                               Logger logger)
            : base(configService, diskProvider, diskTransferService, logger)
        {
            _metadataFactory = metadataFactory;
            _cleanMetadataService = cleanMetadataService;
            _otherExtraFileRenamer = otherExtraFileRenamer;
            _recycleBinProvider = recycleBinProvider;
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _httpClient = httpClient;
            _mediaFileAttributeService = mediaFileAttributeService;
            _metadataFileService = metadataFileService;
            _issueService = issueService;
            _logger = logger;
        }

        public override int Order => 0;

        public override IEnumerable<ExtraFile> CreateAfterSeriesScan(Series series, List<ComicFile> comicFiles)
        {
            var metadataFiles = _metadataFileService.GetFilesBySeries(series.Id);
            _cleanMetadataService.Clean(series);

            if (!_diskProvider.FolderExists(series.Path))
            {
                _logger.Info("Series folder does not exist, skipping metadata creation");
                return Enumerable.Empty<MetadataFile>();
            }

            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {
                var consumerFiles = GetMetadataFilesForConsumer(consumer, metadataFiles);

                files.AddIfNotNull(ProcessSeriesMetadata(consumer, series, consumerFiles));
                files.AddRange(ProcessSeriesImages(consumer, series, consumerFiles));

                foreach (var comicFile in comicFiles)
                {
                    files.AddIfNotNull(ProcessIssueMetadata(consumer, series, comicFile, consumerFiles));
                }
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> CreateAfterComicImport(Series series, ComicFile comicFile)
        {
            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {
                files.AddIfNotNull(ProcessIssueMetadata(consumer, series, comicFile, new List<MetadataFile>()));
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> CreateAfterComicImport(Series series, Issue issue, string seriesFolder, string issueFolder)
        {
            var metadataFiles = _metadataFileService.GetFilesBySeries(series.Id);

            if (seriesFolder.IsNullOrWhiteSpace() && issueFolder.IsNullOrWhiteSpace())
            {
                return new List<MetadataFile>();
            }

            var files = new List<MetadataFile>();

            foreach (var consumer in _metadataFactory.Enabled())
            {
                var consumerFiles = GetMetadataFilesForConsumer(consumer, metadataFiles);

                if (seriesFolder.IsNotNullOrWhiteSpace())
                {
                    files.AddIfNotNull(ProcessSeriesMetadata(consumer, series, consumerFiles));
                    files.AddRange(ProcessSeriesImages(consumer, series, consumerFiles));
                }
            }

            _metadataFileService.Upsert(files);

            return files;
        }

        public override IEnumerable<ExtraFile> MoveFilesAfterRename(Series series, List<ComicFile> comicFiles)
        {
            var metadataFiles = _metadataFileService.GetFilesBySeries(series.Id);
            var movedFiles = new List<MetadataFile>();
            var distinctTrackFilePaths = comicFiles.DistinctBy(s => Path.GetDirectoryName(s.Path)).ToList();

            // TODO: Move EpisodeImage and EpisodeMetadata metadata files, instead of relying on consumers to do it
            // (Xbmc's EpisodeImage is more than just the extension)
            foreach (var consumer in _metadataFactory.GetAvailableProviders())
            {
                foreach (var filePath in distinctTrackFilePaths)
                {
                    var metadataFilesForConsumer = GetMetadataFilesForConsumer(consumer, metadataFiles)
                        .Where(m => m.IssueId == filePath.IssueId)
                        .Where(m => m.Type == MetadataType.IssueImage || m.Type == MetadataType.IssueMetadata)
                        .ToList();

                    foreach (var metadataFile in metadataFilesForConsumer)
                    {
                        var newFileName = consumer.GetFilenameAfterMove(series, Path.GetDirectoryName(filePath.Path), metadataFile);
                        var existingFileName = Path.Combine(series.Path, metadataFile.RelativePath);

                        if (newFileName.PathNotEquals(existingFileName))
                        {
                            try
                            {
                                _diskProvider.MoveFile(existingFileName, newFileName);
                                metadataFile.RelativePath = series.Path.GetRelativePath(newFileName);
                                movedFiles.Add(metadataFile);
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn(ex, "Unable to move metadata file after rename: {0}", existingFileName);
                            }
                        }
                    }
                }

                foreach (var comicFile in comicFiles)
                {
                    var metadataFilesForConsumer = GetMetadataFilesForConsumer(consumer, metadataFiles).Where(m => m.ComicFileId == comicFile.Id).ToList();

                    foreach (var metadataFile in metadataFilesForConsumer)
                    {
                        var newFileName = consumer.GetFilenameAfterMove(series, comicFile, metadataFile);
                        var existingFileName = Path.Combine(series.Path, metadataFile.RelativePath);

                        if (newFileName.PathNotEquals(existingFileName))
                        {
                            try
                            {
                                _diskProvider.MoveFile(existingFileName, newFileName);
                                metadataFile.RelativePath = series.Path.GetRelativePath(newFileName);
                                movedFiles.Add(metadataFile);
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn(ex, "Unable to move metadata file after rename: {0}", existingFileName);
                            }
                        }
                    }
                }
            }

            _metadataFileService.Upsert(movedFiles);

            return movedFiles;
        }

        public override ExtraFile Import(Series series, ComicFile comicFile, string path, string extension, bool readOnly)
        {
            return null;
        }

        private List<MetadataFile> GetMetadataFilesForConsumer(IMetadata consumer, List<MetadataFile> seriesMetadata)
        {
            return seriesMetadata.Where(c => c.Consumer == consumer.GetType().Name).ToList();
        }

        private MetadataFile ProcessSeriesMetadata(IMetadata consumer, Series series, List<MetadataFile> existingMetadataFiles)
        {
            var seriesMetadata = consumer.SeriesMetadata(series);

            if (seriesMetadata == null)
            {
                return null;
            }

            var hash = seriesMetadata.Contents.SHA256Hash();

            var metadata = GetMetadataFile(series, existingMetadataFiles, e => e.Type == MetadataType.SeriesMetadata) ??
                               new MetadataFile
                               {
                                   SeriesId = series.Id,
                                   Consumer = consumer.GetType().Name,
                                   Type = MetadataType.SeriesMetadata
                               };

            if (hash == metadata.Hash)
            {
                if (seriesMetadata.RelativePath != metadata.RelativePath)
                {
                    metadata.RelativePath = seriesMetadata.RelativePath;

                    return metadata;
                }

                return null;
            }

            var fullPath = Path.Combine(series.Path, seriesMetadata.RelativePath);

            _otherExtraFileRenamer.RenameOtherExtraFile(series, fullPath);

            _logger.Debug("Writing Series Metadata to: {0}", fullPath);
            SaveMetadataFile(fullPath, seriesMetadata.Contents);

            metadata.Hash = hash;
            metadata.RelativePath = seriesMetadata.RelativePath;
            metadata.Extension = Path.GetExtension(fullPath);

            return metadata;
        }

        private MetadataFile ProcessIssueMetadata(IMetadata consumer, Series series, ComicFile comicFile, List<MetadataFile> existingMetadataFiles)
        {
            var trackMetadata = consumer.IssueMetadata(series, comicFile);

            if (trackMetadata == null)
            {
                return null;
            }

            var fullPath = Path.Combine(series.Path, trackMetadata.RelativePath);

            _otherExtraFileRenamer.RenameOtherExtraFile(series, fullPath);

            var existingMetadata = GetMetadataFile(series, existingMetadataFiles, c => c.Type == MetadataType.IssueMetadata &&
                                                                                  c.ComicFileId == comicFile.Id);

            if (existingMetadata != null)
            {
                var existingFullPath = Path.Combine(series.Path, existingMetadata.RelativePath);
                if (fullPath.PathNotEquals(existingFullPath))
                {
                    _diskTransferService.TransferFile(existingFullPath, fullPath, TransferMode.Move);
                    existingMetadata.RelativePath = trackMetadata.RelativePath;
                }
            }

            var hash = trackMetadata.Contents.SHA256Hash();

            var metadata = existingMetadata ??
                           new MetadataFile
                           {
                               SeriesId = series.Id,
                               IssueId = comicFile.IssueId,
                               ComicFileId = comicFile.Id,
                               Consumer = consumer.GetType().Name,
                               Type = MetadataType.IssueMetadata,
                               RelativePath = trackMetadata.RelativePath,
                               Extension = Path.GetExtension(fullPath)
                           };

            if (hash == metadata.Hash)
            {
                return null;
            }

            _logger.Debug("Writing Track Metadata to: {0}", fullPath);
            SaveMetadataFile(fullPath, trackMetadata.Contents);

            metadata.Hash = hash;

            return metadata;
        }

        private List<MetadataFile> ProcessSeriesImages(IMetadata consumer, Series series, List<MetadataFile> existingMetadataFiles)
        {
            var result = new List<MetadataFile>();

            foreach (var image in consumer.SeriesImages(series))
            {
                var fullPath = Path.Combine(series.Path, image.RelativePath);

                if (_diskProvider.FileExists(fullPath))
                {
                    _logger.Debug("Series image already exists: {0}", fullPath);
                    continue;
                }

                _otherExtraFileRenamer.RenameOtherExtraFile(series, fullPath);

                var metadata = GetMetadataFile(series, existingMetadataFiles, c => c.Type == MetadataType.SeriesImage &&
                                                                              c.RelativePath == image.RelativePath) ??
                               new MetadataFile
                               {
                                   SeriesId = series.Id,
                                   Consumer = consumer.GetType().Name,
                                   Type = MetadataType.SeriesImage,
                                   RelativePath = image.RelativePath,
                                   Extension = Path.GetExtension(fullPath)
                               };

                DownloadImage(series, image);

                result.Add(metadata);
            }

            return result;
        }

        private void DownloadImage(Series series, ImageFileResult image)
        {
            var fullPath = Path.Combine(series.Path, image.RelativePath);
            var downloaded = true;

            try
            {
                if (image.Url.StartsWith("http"))
                {
                    _httpClient.DownloadFile(image.Url, fullPath);
                }
                else if (_diskProvider.FileExists(image.Url))
                {
                    _diskProvider.CopyFile(image.Url, fullPath);
                }
                else
                {
                    downloaded = false;
                }

                if (downloaded)
                {
                    _mediaFileAttributeService.SetFilePermissions(fullPath);
                }
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex, "Couldn't download image {0} for {1}. {2}", image.Url, series, ex.Message);
            }
            catch (WebException ex)
            {
                _logger.Warn(ex, "Couldn't download image {0} for {1}. {2}", image.Url, series, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Couldn't download image {0} for {1}", image.Url, series);
            }
        }

        private void SaveMetadataFile(string path, string contents)
        {
            _diskProvider.WriteAllText(path, contents);
            _mediaFileAttributeService.SetFilePermissions(path);
        }

        private MetadataFile GetMetadataFile(Series series, List<MetadataFile> existingMetadataFiles, Func<MetadataFile, bool> predicate)
        {
            var matchingMetadataFiles = existingMetadataFiles.Where(predicate).ToList();

            if (matchingMetadataFiles.Empty())
            {
                return null;
            }

            //Remove duplicate metadata files from DB and disk
            foreach (var file in matchingMetadataFiles.Skip(1))
            {
                var path = Path.Combine(series.Path, file.RelativePath);

                _logger.Debug("Removing duplicate Metadata file: {0}", path);

                var subfolder = _diskProvider.GetParentFolder(series.Path).GetRelativePath(_diskProvider.GetParentFolder(path));
                _recycleBinProvider.DeleteFile(path, subfolder);
                _metadataFileService.Delete(file.Id);
            }

            return matchingMetadataFiles.First();
        }
    }
}
