using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.ComicInfo;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMetadataTagService
    {
        ParsedTrackInfo ReadTags(IFileInfo file);
        void WriteTags(ComicFile comicFile, bool newDownload, bool force = false);
        void SyncTags(List<Issue> issues);
        List<RetagComicFilePreview> GetRetagPreviewsBySeries(int seriesId);
        List<RetagComicFilePreview> GetRetagPreviewsByIssue(int seriesId);
    }

    public class MetadataTagService : IMetadataTagService,
        IExecute<RetagFilesCommand>,
        IExecute<RetagSeriesCommand>
    {
        private readonly IAudioTagService _audioTagService;
        private readonly IComicInfoReaderService _comicInfoReaderService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public MetadataTagService(IAudioTagService audioTagService,
            IComicInfoReaderService comicInfoReaderService,
            IMediaFileService mediaFileService,
            IEventAggregator eventAggregator,
            Logger logger)
        {
            _audioTagService = audioTagService;
            _comicInfoReaderService = comicInfoReaderService;
            _mediaFileService = mediaFileService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public ParsedTrackInfo ReadTags(IFileInfo file)
        {
            if (MediaFileExtensions.AudioExtensions.Contains(file.Extension))
            {
                return _audioTagService.ReadTags(file.FullName);
            }

            return ReadComicTags(file);
        }

        private ParsedTrackInfo ReadComicTags(IFileInfo file)
        {
            var info = new ParsedTrackInfo();

            try
            {
                var ident = _comicInfoReaderService.ReadIdentificationFromPath(file.FullName);
                if (ident == null || !ident.HasAny)
                {
                    return info;
                }

                if (!string.IsNullOrWhiteSpace(ident.Series))
                {
                    info.Series = new List<string> { ident.Series };
                    info.SeriesTitle = ident.Series;
                    info.CleanTitle = ident.Series.CleanSeriesName();
                }

                if (!string.IsNullOrWhiteSpace(ident.Title))
                {
                    info.IssueTitle = ident.Title;
                    info.Title = ident.Title;
                }

                if (!string.IsNullOrWhiteSpace(ident.Number))
                {
                    info.SeriesIndex = ident.Number;
                }

                if (!string.IsNullOrWhiteSpace(ident.Year)
                    && uint.TryParse(ident.Year, out var year))
                {
                    info.Year = year;
                }

                if (!string.IsNullOrWhiteSpace(ident.Publisher))
                {
                    info.Publisher = ident.Publisher;
                }

                _logger.Debug(
                    "Read ComicInfo identification from {0}: series='{1}', issue='{2}', number='{3}'",
                    file.Name,
                    ident.Series,
                    ident.Title,
                    ident.Number);
            }
            catch (System.Exception ex)
            {
                _logger.Warn(ex, "Failed to read ComicInfo from {0}", file.FullName);
            }

            return info;
        }

        public void WriteTags(ComicFile comicFile, bool newDownload, bool force = false)
        {
            var extension = Path.GetExtension(comicFile.Path);
            if (MediaFileExtensions.AudioExtensions.Contains(extension))
            {
                _audioTagService.WriteTags(comicFile, newDownload, force);
            }
        }

        public void SyncTags(List<Issue> issues)
        {
            _audioTagService.SyncTags(issues);
        }

        public List<RetagComicFilePreview> GetRetagPreviewsBySeries(int seriesId)
        {
            return _audioTagService.GetRetagPreviewsBySeries(seriesId);
        }

        public List<RetagComicFilePreview> GetRetagPreviewsByIssue(int issueId)
        {
            return _audioTagService.GetRetagPreviewsByIssue(issueId);
        }

        public void Execute(RetagFilesCommand message)
        {
            // Re-embed ComicInfo.xml for comic files
            var comicFileIds = new List<int>();
            foreach (var fileId in message.Files)
            {
                try
                {
                    var comicFile = _mediaFileService.Get(fileId);
                    if (comicFile.ComicFormat != ComicFormat.Unknown)
                    {
                        _eventAggregator.PublishEvent(new ComicFileAddedEvent(comicFile));
                        comicFileIds.Add(fileId);
                    }
                }
                catch (NzbDrone.Core.Datastore.ModelNotFoundException)
                {
                    _logger.Warn("ComicFile {0} not found, skipping retag", fileId);
                }
            }

            // Only pass non-comic files to the audio tag service
            var audioFiles = message.Files.Except(comicFileIds).ToList();
            if (audioFiles.Any())
            {
                _audioTagService.RetagFiles(message);
            }
        }

        public void Execute(RetagSeriesCommand message)
        {
            _audioTagService.RetagSeries(message);
        }
    }
}
