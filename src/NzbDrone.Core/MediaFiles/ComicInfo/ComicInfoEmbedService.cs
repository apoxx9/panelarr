using System.IO;
using System.IO.Compression;
using NLog;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles.ComicInfo
{
    public class ComicInfoEmbedService :
        IHandle<ComicFileAddedEvent>,
        IHandle<ComicFileRenamedEvent>
    {
        private const string ComicInfoFileName = "ComicInfo.xml";

        private readonly IComicInfoGenerator _generator;
        private readonly IBookService _bookService;
        private readonly IPublisherService _publisherService;
        private readonly Logger _logger;

        public ComicInfoEmbedService(
            IComicInfoGenerator generator,
            IBookService bookService,
            IPublisherService publisherService,
            Logger logger)
        {
            _generator = generator;
            _bookService = bookService;
            _publisherService = publisherService;
            _logger = logger;
        }

        public void Handle(ComicFileAddedEvent message)
        {
            EmbedComicInfo(message.ComicFile);
        }

        public void Handle(ComicFileRenamedEvent message)
        {
            EmbedComicInfo(message.ComicFile);
        }

        private void EmbedComicInfo(ComicFile comicFile)
        {
            if (comicFile.ComicFormat != Books.ComicFormat.CBZ)
            {
                _logger.Debug("Skipping ComicInfo.xml embedding for non-CBZ file: {0}", comicFile.Path);
                return;
            }

            if (!File.Exists(comicFile.Path))
            {
                _logger.Warn("Comic file not found, skipping ComicInfo.xml embedding: {0}", comicFile.Path);
                return;
            }

            var issue = comicFile.Issue?.Value ?? _bookService.GetBook(comicFile.IssueId);
            if (issue == null)
            {
                _logger.Warn("Issue not found for ComicFile {0}, skipping ComicInfo.xml embedding", comicFile.Id);
                return;
            }

            var seriesMetadata = issue.SeriesMetadata?.Value;
            Publisher publisher = null;

            if (seriesMetadata?.PublisherId.HasValue == true)
            {
                publisher = _publisherService.GetPublisher(seriesMetadata.PublisherId.Value);
            }

            var xmlContent = _generator.Generate(issue, seriesMetadata, publisher);

            try
            {
                using var archive = ZipFile.Open(comicFile.Path, ZipArchiveMode.Update);

                var existing = archive.GetEntry(ComicInfoFileName);
                existing?.Delete();

                var entry = archive.CreateEntry(ComicInfoFileName, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream);
                writer.Write(xmlContent);

                _logger.Debug("Embedded ComicInfo.xml into {0}", comicFile.Path);
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex, "Failed to embed ComicInfo.xml into {0}", comicFile.Path);
            }
        }
    }
}
