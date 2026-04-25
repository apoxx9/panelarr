using System.Collections.Generic;
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
        private readonly IComicInfoReaderService _comicInfoReaderService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public MetadataTagService(IComicInfoReaderService comicInfoReaderService,
            IMediaFileService mediaFileService,
            IEventAggregator eventAggregator,
            Logger logger)
        {
            _comicInfoReaderService = comicInfoReaderService;
            _mediaFileService = mediaFileService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public ParsedTrackInfo ReadTags(IFileInfo file)
        {
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
        }

        public void SyncTags(List<Issue> issues)
        {
            // Comic files use ComicInfo.xml embedded during import — no audio sync needed
        }

        public List<RetagComicFilePreview> GetRetagPreviewsBySeries(int seriesId)
        {
            // Comic file retag previews not yet implemented (requires ComicInfo.xml writer)
            return new List<RetagComicFilePreview>();
        }

        public List<RetagComicFilePreview> GetRetagPreviewsByIssue(int issueId)
        {
            // Comic file retag previews not yet implemented (requires ComicInfo.xml writer)
            return new List<RetagComicFilePreview>();
        }

        public void Execute(RetagFilesCommand message)
        {
            foreach (var fileId in message.Files)
            {
                try
                {
                    var comicFile = _mediaFileService.Get(fileId);
                    if (comicFile.ComicFormat != ComicFormat.Unknown)
                    {
                        _eventAggregator.PublishEvent(new ComicFileAddedEvent(comicFile));
                    }
                }
                catch (NzbDrone.Core.Datastore.ModelNotFoundException)
                {
                    _logger.Warn("ComicFile {0} not found, skipping retag", fileId);
                }
            }
        }

        public void Execute(RetagSeriesCommand message)
        {
            var allSeries = message.SeriesIds;

            foreach (var seriesId in allSeries)
            {
                var comicFiles = _mediaFileService.GetFilesBySeries(seriesId);

                _logger.Info("Re-tagging {0} comic files for series {1}", comicFiles.Count, seriesId);

                foreach (var file in comicFiles.Where(x => x.ComicFormat != ComicFormat.Unknown))
                {
                    _eventAggregator.PublishEvent(new ComicFileAddedEvent(file));
                }
            }
        }
    }
}
