using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.ComicInfo;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class MetadataTagServiceFixture : CoreTest<MetadataTagService>
    {
        private Series _series;
        private ComicFile _comicFile;
        private Issue _issue;

        [SetUp]
        public void Setup()
        {
            _series = new Series { Id = 5, Name = "Nightwing" };

            var path = GetTempFilePath();
            File.WriteAllText(path, "cbz stand-in");

            _issue = new Issue { Id = 12, SeriesId = 5, IssueNumber = "1" };

            _comicFile = new ComicFile
            {
                Id = 3,
                IssueId = 12,
                Path = path,
                ComicFormat = ComicFormat.CBZ,
                Series = _series,
                Issue = _issue
            };

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.Get(_comicFile.Id))
                  .Returns(_comicFile);

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesBySeries(_series.Id))
                  .Returns(new List<ComicFile> { _comicFile });

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssue(_issue.Id))
                  .Returns(_issue);

            GivenTags(current: new Dictionary<string, string> { { "Title", "Old" } },
                      generated: new Dictionary<string, string> { { "Title", "New" } },
                      hasMetronInfo: true);
        }

        private void GivenTags(Dictionary<string, string> current, Dictionary<string, string> generated, bool hasMetronInfo)
        {
            var results = new List<ComicMetadataResult>();

            if (current != null)
            {
                results.Add(new ComicMetadataResult { Source = "ComicInfo.xml", Fields = current });
            }

            if (hasMetronInfo)
            {
                results.Add(new ComicMetadataResult { Source = "MetronInfo.xml", Fields = new Dictionary<string, string>() });
            }

            Mocker.GetMock<IComicInfoReaderService>()
                  .Setup(s => s.ReadMetadata(_comicFile))
                  .Returns(results);

            Mocker.GetMock<IComicInfoGenerator>()
                  .Setup(s => s.Generate(_issue, It.IsAny<SeriesMetadata>(), It.IsAny<Publisher>()))
                  .Returns("<ComicInfo/>");

            Mocker.GetMock<IComicInfoReaderService>()
                  .Setup(s => s.ParseXmlContent("<ComicInfo/>", "ComicInfo.xml"))
                  .Returns(new ComicMetadataResult { Source = "ComicInfo.xml", Fields = generated });
        }

        [Test]
        public void retag_files_command_should_embed_and_publish_event_with_diff()
        {
            Subject.Execute(new RetagFilesCommand { Files = new List<int> { _comicFile.Id } });

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(_comicFile), Times.Once());

            VerifyRetaggedPublished(e =>
                e.ComicFile == _comicFile &&
                e.Series == _series &&
                e.Scrubbed &&
                e.Diff.ContainsKey("Title") &&
                e.Diff["Title"].Item1 == "Old" &&
                e.Diff["Title"].Item2 == "New");
        }

        [Test]
        public void retag_series_command_should_embed_and_publish_event()
        {
            Subject.Execute(new RetagSeriesCommand { SeriesIds = new List<int> { _series.Id } });

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(_comicFile), Times.Once());

            VerifyRetaggedPublished(e => e.ComicFile == _comicFile);
        }

        [Test]
        public void should_publish_events_only_after_all_embeds_have_finished()
        {
            var second = new ComicFile
            {
                Id = 4,
                IssueId = 12,
                Path = GetTempFilePath(),
                ComicFormat = ComicFormat.CBZ,
                Series = _series,
                Issue = _issue
            };

            File.WriteAllText(second.Path, "cbz stand-in");

            Mocker.GetMock<IComicInfoReaderService>()
                  .Setup(s => s.ReadMetadata(second))
                  .Returns(new List<ComicMetadataResult> { new ComicMetadataResult { Source = "ComicInfo.xml", Fields = new Dictionary<string, string> { { "Title", "Old" } } } });

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesBySeries(_series.Id))
                  .Returns(new List<ComicFile> { _comicFile, second });

            var embeds = 0;
            var embedsAtFirstPublish = -1;

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Setup(s => s.EmbedMetadata(It.IsAny<ComicFile>()))
                  .Callback(() => embeds++);

            Mocker.GetMock<NzbDrone.Core.Messaging.Events.IEventAggregator>()
                  .Setup(s => s.PublishEvent(It.IsAny<ComicFileRetaggedEvent>()))
                  .Callback(() =>
                  {
                      if (embedsAtFirstPublish < 0)
                      {
                          embedsAtFirstPublish = embeds;
                      }
                  });

            Subject.Execute(new RetagSeriesCommand { SeriesIds = new List<int> { _series.Id } });

            embeds.Should().Be(2);
            embedsAtFirstPublish.Should().Be(2);
        }

        [Test]
        public void should_skip_embed_and_event_when_tags_are_already_current()
        {
            GivenTags(current: new Dictionary<string, string> { { "Title", "Same" } },
                      generated: new Dictionary<string, string> { { "Title", "Same" } },
                      hasMetronInfo: true);

            Subject.Execute(new RetagFilesCommand { Files = new List<int> { _comicFile.Id } });

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(It.IsAny<ComicFile>()), Times.Never());

            VerifyEventNotPublished<ComicFileRetaggedEvent>();
        }

        [Test]
        public void should_embed_when_comicinfo_is_current_but_metroninfo_is_missing()
        {
            GivenTags(current: new Dictionary<string, string> { { "Title", "Same" } },
                      generated: new Dictionary<string, string> { { "Title", "Same" } },
                      hasMetronInfo: false);

            Subject.Execute(new RetagFilesCommand { Files = new List<int> { _comicFile.Id } });

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(_comicFile), Times.Once());

            VerifyRetaggedPublished(e => e.Diff.Count == 0);
        }

        [Test]
        public void should_report_unscrubbed_when_file_had_no_existing_comicinfo()
        {
            GivenTags(current: null,
                      generated: new Dictionary<string, string> { { "Title", "New" } },
                      hasMetronInfo: false);

            Subject.Execute(new RetagFilesCommand { Files = new List<int> { _comicFile.Id } });

            VerifyRetaggedPublished(e => !e.Scrubbed && e.Diff["Title"].Item2 == "New");
        }

        [Test]
        public void should_embed_with_empty_diff_when_existing_tags_cannot_be_read()
        {
            Mocker.GetMock<IComicInfoReaderService>()
                  .Setup(s => s.ReadMetadata(_comicFile))
                  .Throws(new InvalidDataException("corrupt archive"));

            Subject.Execute(new RetagFilesCommand { Files = new List<int> { _comicFile.Id } });

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(_comicFile), Times.Once());

            VerifyRetaggedPublished(e => e.Diff.Count == 0 && !e.Scrubbed);

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_skip_files_that_are_not_cbz()
        {
            _comicFile.ComicFormat = ComicFormat.CBR;

            Subject.Execute(new RetagFilesCommand { Files = new List<int> { _comicFile.Id } });

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(It.IsAny<ComicFile>()), Times.Never());

            VerifyEventNotPublished<ComicFileRetaggedEvent>();
        }

        [Test]
        public void should_skip_files_missing_from_disk()
        {
            File.Delete(_comicFile.Path);

            Subject.Execute(new RetagFilesCommand { Files = new List<int> { _comicFile.Id } });

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(It.IsAny<ComicFile>()), Times.Never());

            VerifyEventNotPublished<ComicFileRetaggedEvent>();
        }

        [Test]
        public void should_skip_unmapped_files()
        {
            _comicFile.IssueId = 0;

            Subject.Execute(new RetagFilesCommand { Files = new List<int> { _comicFile.Id } });

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(It.IsAny<ComicFile>()), Times.Never());

            VerifyEventNotPublished<ComicFileRetaggedEvent>();
        }

        [Test]
        public void should_continue_past_missing_files_in_a_batch()
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.Get(99))
                  .Throws(new ModelNotFoundException(typeof(ComicFile), 99));

            Subject.Execute(new RetagFilesCommand { Files = new List<int> { 99, _comicFile.Id } });

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(_comicFile), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void write_tags_should_not_publish_retagged_events()
        {
            Subject.WriteTags(_comicFile, newDownload: true);

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(_comicFile), Times.Once());

            VerifyEventNotPublished<ComicFileRetaggedEvent>();
        }

        private void VerifyRetaggedPublished(Func<ComicFileRetaggedEvent, bool> predicate)
        {
            Mocker.GetMock<NzbDrone.Core.Messaging.Events.IEventAggregator>()
                  .Verify(s => s.PublishEvent(It.Is<ComicFileRetaggedEvent>(e => predicate(e))), Times.Once());
        }
    }
}
