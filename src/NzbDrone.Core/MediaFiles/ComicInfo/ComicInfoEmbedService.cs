using System.IO;
using System.IO.Compression;
using NLog;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles.ComicInfo
{
    public interface IComicInfoEmbedService
    {
        void EmbedMetadata(ComicFile comicFile);
    }

    public class ComicInfoEmbedService :
        IComicInfoEmbedService,
        IHandle<ComicFileAddedEvent>,
        IHandle<ComicFileRenamedEvent>
    {
        private const string ComicInfoFileName = "ComicInfo.xml";
        private const string MetronInfoFileName = "MetronInfo.xml";

        private readonly IComicInfoGenerator _generator;
        private readonly IMetronInfoGenerator _metronInfoGenerator;
        private readonly IIssueService _issueService;
        private readonly IPublisherService _publisherService;
        private readonly Logger _logger;

        public ComicInfoEmbedService(
            IComicInfoGenerator generator,
            IMetronInfoGenerator metronInfoGenerator,
            IIssueService issueService,
            IPublisherService publisherService,
            Logger logger)
        {
            _generator = generator;
            _metronInfoGenerator = metronInfoGenerator;
            _issueService = issueService;
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

        public void EmbedMetadata(ComicFile comicFile)
        {
            EmbedComicInfo(comicFile);
        }

        private void EmbedComicInfo(ComicFile comicFile)
        {
            if (comicFile.ComicFormat != Issues.ComicFormat.CBZ)
            {
                _logger.Debug("Skipping ComicInfo.xml embedding for non-CBZ file: {0}", comicFile.Path);
                return;
            }

            if (!File.Exists(comicFile.Path))
            {
                _logger.Warn("Comic file not found, skipping ComicInfo.xml embedding: {0}", comicFile.Path);
                return;
            }

            var issue = comicFile.Issue?.Value ?? _issueService.GetIssue(comicFile.IssueId);
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
            var metronXmlContent = _metronInfoGenerator.Generate(issue, seriesMetadata, publisher);

            try
            {
                using var archive = ZipFile.Open(comicFile.Path, ZipArchiveMode.Update);

                // Embed ComicInfo.xml
                var existingComicInfo = archive.GetEntry(ComicInfoFileName);
                existingComicInfo?.Delete();

                var comicInfoEntry = archive.CreateEntry(ComicInfoFileName, CompressionLevel.Fastest);
                using (var entryStream = comicInfoEntry.Open())
                using (var writer = new StreamWriter(entryStream))
                {
                    writer.Write(xmlContent);
                }

                _logger.Debug("Embedded ComicInfo.xml into {0}", comicFile.Path);

                // Embed MetronInfo.xml
                var existingMetronInfo = archive.GetEntry(MetronInfoFileName);
                existingMetronInfo?.Delete();

                var metronInfoEntry = archive.CreateEntry(MetronInfoFileName, CompressionLevel.Fastest);
                using (var metronStream = metronInfoEntry.Open())
                using (var metronWriter = new StreamWriter(metronStream))
                {
                    metronWriter.Write(metronXmlContent);
                }

                _logger.Debug("Embedded MetronInfo.xml into {0}", comicFile.Path);
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex, "Failed to embed metadata into {0}", comicFile.Path);
            }
        }
    }
}
