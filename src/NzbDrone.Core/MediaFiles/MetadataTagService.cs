using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using NLog;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMetadataTagService
    {
        ParsedTrackInfo ReadTags(IFileInfo file);
        void WriteTags(ComicFile trackfile, bool newDownload, bool force = false);
        void SyncTags(List<Issue> issues);
        List<RetagComicFilePreview> GetRetagPreviewsBySeries(int seriesId);
        List<RetagComicFilePreview> GetRetagPreviewsByIssue(int seriesId);
    }

    public class MetadataTagService : IMetadataTagService,
        IExecute<RetagFilesCommand>,
        IExecute<RetagSeriesCommand>
    {
        private readonly IAudioTagService _audioTagService;
        private readonly Logger _logger;

        public MetadataTagService(IAudioTagService audioTagService,
            Logger logger)
        {
            _audioTagService = audioTagService;
            _logger = logger;
        }

        public ParsedTrackInfo ReadTags(IFileInfo file)
        {
            if (MediaFileExtensions.AudioExtensions.Contains(file.Extension))
            {
                return _audioTagService.ReadTags(file.FullName);
            }
            else
            {
                return new ParsedTrackInfo();
            }
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
            _audioTagService.RetagFiles(message);
        }

        public void Execute(RetagSeriesCommand message)
        {
            _audioTagService.RetagSeries(message);
        }
    }
}
