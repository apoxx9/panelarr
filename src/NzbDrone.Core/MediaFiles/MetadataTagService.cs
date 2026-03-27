using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using NLog;
using NzbDrone.Core.Books;
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
        List<RetagComicFilePreview> GetRetagPreviewsBySeries(int authorId);
        List<RetagComicFilePreview> GetRetagPreviewsByBook(int authorId);
    }

    public class MetadataTagService : IMetadataTagService,
        IExecute<RetagFilesCommand>,
        IExecute<RetagSeriesCommand>
    {
        private readonly IAudioTagService _audioTagService;
        private readonly IEBookTagService _eBookTagService;
        private readonly Logger _logger;

        public MetadataTagService(IAudioTagService audioTagService,
            IEBookTagService eBookTagService,
            Logger logger)
        {
            _audioTagService = audioTagService;
            _eBookTagService = eBookTagService;

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
                return _eBookTagService.ReadTags(file);
            }
        }

        public void WriteTags(ComicFile comicFile, bool newDownload, bool force = false)
        {
            var extension = Path.GetExtension(comicFile.Path);
            if (MediaFileExtensions.AudioExtensions.Contains(extension))
            {
                _audioTagService.WriteTags(comicFile, newDownload, force);
            }
            else
            {
                _eBookTagService.WriteTags(comicFile, newDownload, force);
            }
        }

        public void SyncTags(List<Issue> issues)
        {
            _audioTagService.SyncTags(issues);
            _eBookTagService.SyncTags(issues);
        }

        public List<RetagComicFilePreview> GetRetagPreviewsBySeries(int authorId)
        {
            var previews = _audioTagService.GetRetagPreviewsBySeries(authorId);
            previews.AddRange(_eBookTagService.GetRetagPreviewsBySeries(authorId));

            return previews;
        }

        public List<RetagComicFilePreview> GetRetagPreviewsByBook(int bookId)
        {
            var previews = _audioTagService.GetRetagPreviewsByBook(bookId);
            previews.AddRange(_eBookTagService.GetRetagPreviewsByBook(bookId));

            return previews;
        }

        public void Execute(RetagFilesCommand message)
        {
            _eBookTagService.RetagFiles(message);
            _audioTagService.RetagFiles(message);
        }

        public void Execute(RetagSeriesCommand message)
        {
            _eBookTagService.RetagSeries(message);
            _audioTagService.RetagSeries(message);
        }
    }
}
