using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.ComicInfo;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles
{
    // Converts a series' archives to real ZIP .cbz files and embeds provider
    // metadata into the mapped ones — Mylar's convert-before-tag convention,
    // since nothing can write into a RAR. Covers every file under the series'
    // folder, so unmapped rows (zip-content files mislabeled .cbr that no scan
    // could read) are normalized too and become identifiable.
    public class ComicFileConversionService : IExecute<ConvertComicFilesCommand>
    {
        private readonly ISeriesService _seriesService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IComicFormatConverter _converter;
        private readonly IComicInfoEmbedService _embedService;
        private readonly IFileSystem _fileSystem;
        private readonly Logger _logger;

        public ComicFileConversionService(ISeriesService seriesService,
                                          IMediaFileService mediaFileService,
                                          IComicFormatConverter converter,
                                          IComicInfoEmbedService embedService,
                                          IFileSystem fileSystem,
                                          Logger logger)
        {
            _seriesService = seriesService;
            _mediaFileService = mediaFileService;
            _converter = converter;
            _embedService = embedService;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public void Execute(ConvertComicFilesCommand message)
        {
            if (message.SeriesIds == null || message.SeriesIds.Count == 0)
            {
                _logger.Warn("Convert command received no series ids");
                return;
            }

            // Series may share a folder (annuals, collection lines), so the
            // same file can surface for several series - convert once
            var files = new Dictionary<int, ComicFile>();

            foreach (var seriesId in message.SeriesIds.Distinct())
            {
                var series = _seriesService.GetSeries(seriesId);

                if (series == null)
                {
                    continue;
                }

                foreach (var file in _mediaFileService.GetFilesWithBasePath(series.Path))
                {
                    files[file.Id] = file;
                }
            }

            var converted = 0;
            var tagged = 0;
            var failed = 0;
            var processed = 0;
            var total = files.Count;

            foreach (var file in files.Values)
            {
                processed++;
                _logger.ProgressInfo("Converting archives {0}/{1}", processed, total);

                var result = _converter.ConvertToRealCbz(file.Path);

                if (result.Error != null)
                {
                    _logger.Warn("Could not convert {0}: {1}", file.Path, result.Error);
                    failed++;
                    continue;
                }

                if (result.Changed)
                {
                    file.Path = result.FinalPath;
                    file.ComicFormat = ComicFormat.CBZ;

                    var info = _fileSystem.FileInfo.FromFileName(result.FinalPath);
                    file.Size = info.Length;
                    file.Modified = info.LastWriteTimeUtc;

                    _mediaFileService.Update(file);
                    converted++;
                }

                // Explicit user command - embed provider metadata regardless
                // of the WriteIssueTags setting (mapped CBZ files only; the
                // embed service skips the rest)
                if (file.IssueId > 0 && file.ComicFormat == ComicFormat.CBZ)
                {
                    _embedService.EmbedMetadata(file);
                    tagged++;
                }
            }

            _logger.ProgressInfo("Conversion complete: {0} converted, {1} tagged, {2} failed of {3} files", converted, tagged, failed, total);
        }
    }
}
