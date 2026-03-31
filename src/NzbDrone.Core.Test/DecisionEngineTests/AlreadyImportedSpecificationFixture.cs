using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.History;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class AlreadyImportedSpecificationFixture : CoreTest<AlreadyImportedSpecification>
    {
        private const int FIRST_ALBUM_ID = 1;
        private const string TITLE = "Some.Series-Some.Issue-2018-320kbps-CD-Panelarr";

        private Series _series;
        private QualityModel _mp3;
        private QualityModel _flac;
        private RemoteIssue _remoteIssue;
        private List<EntityHistory> _history;
        private ComicFile _firstFile;

        [SetUp]
        public void Setup()
        {
            var singleIssueList = new List<Issue>
                                    {
                                        new Issue
                                        {
                                            Id = FIRST_ALBUM_ID,
                                            Title = "Some Issue"
                                        }
                                    };

            _series = Builder<Series>.CreateNew()
                                     .Build();

            _firstFile = new ComicFile { Quality = new QualityModel(Quality.CBZ_HD, new Revision(version: 2)), DateAdded = DateTime.Now };

            _mp3 = new QualityModel(Quality.CBR, new Revision(version: 1));
            _flac = new QualityModel(Quality.CBZ_HD, new Revision(version: 1));

            _remoteIssue = new RemoteIssue
            {
                Series = _series,
                ParsedIssueInfo = new ParsedIssueInfo { Quality = _mp3 },
                Issues = singleIssueList,
                Release = Builder<ReleaseInfo>.CreateNew()
                                              .Build()
            };

            _history = new List<EntityHistory>();

            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.EnableCompletedDownloadHandling)
                  .Returns(true);

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.GetByIssue(It.IsAny<int>(), null))
                  .Returns(_history);

            Mocker.GetMock<IMediaFileService>()
                  .Setup(c => c.GetFilesByIssue(It.IsAny<int>()))
                  .Returns(new List<ComicFile> { _firstFile });
        }

        private void GivenCdhDisabled()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.EnableCompletedDownloadHandling)
                  .Returns(false);
        }

        private void GivenHistoryItem(string downloadId, string sourceTitle, QualityModel quality, EntityHistoryEventType eventType)
        {
            _history.Add(new EntityHistory
            {
                DownloadId = downloadId,
                SourceTitle = sourceTitle,
                Quality = quality,
                Date = DateTime.UtcNow,
                EventType = eventType
            });
        }

        [Test]
        public void should_be_accepted_if_CDH_is_disabled()
        {
            GivenCdhDisabled();

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_book_does_not_have_a_file()
        {
            Mocker.GetMock<IMediaFileService>()
                .Setup(c => c.GetFilesByIssue(It.IsAny<int>()))
                .Returns(new List<ComicFile> { });

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_book_does_not_have_grabbed_event()
        {
            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_book_does_not_have_imported_event()
        {
            GivenHistoryItem(Guid.NewGuid().ToString().ToUpper(), TITLE, _mp3, EntityHistoryEventType.Grabbed);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_grabbed_and_imported_quality_is_the_same()
        {
            var downloadId = Guid.NewGuid().ToString().ToUpper();

            GivenHistoryItem(downloadId, TITLE, _mp3, EntityHistoryEventType.Grabbed);
            GivenHistoryItem(downloadId, TITLE, _mp3, EntityHistoryEventType.ComicFileImported);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_rejected_if_grabbed_download_id_matches_release_torrent_hash()
        {
            var downloadId = Guid.NewGuid().ToString().ToUpper();

            GivenHistoryItem(downloadId, TITLE, _mp3, EntityHistoryEventType.Grabbed);
            GivenHistoryItem(downloadId, TITLE, _flac, EntityHistoryEventType.ComicFileImported);

            _remoteIssue.Release = Builder<TorrentInfo>.CreateNew()
                                                         .With(t => t.DownloadProtocol = DownloadProtocol.Torrent)
                                                         .With(t => t.InfoHash = downloadId)
                                                         .Build();

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_accepted_if_release_torrent_hash_is_null()
        {
            var downloadId = Guid.NewGuid().ToString().ToUpper();

            GivenHistoryItem(downloadId, TITLE, _mp3, EntityHistoryEventType.Grabbed);
            GivenHistoryItem(downloadId, TITLE, _flac, EntityHistoryEventType.ComicFileImported);

            _remoteIssue.Release = Builder<TorrentInfo>.CreateNew()
                                                         .With(t => t.DownloadProtocol = DownloadProtocol.Torrent)
                                                         .With(t => t.InfoHash = null)
                                                         .Build();

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_accepted_if_release_torrent_hash_is_null_and_downloadId_is_null()
        {
            GivenHistoryItem(null, TITLE, _mp3, EntityHistoryEventType.Grabbed);
            GivenHistoryItem(null, TITLE, _flac, EntityHistoryEventType.ComicFileImported);

            _remoteIssue.Release = Builder<TorrentInfo>.CreateNew()
                                                         .With(t => t.DownloadProtocol = DownloadProtocol.Torrent)
                                                         .With(t => t.InfoHash = null)
                                                         .Build();

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_rejected_if_release_title_matches_grabbed_event_source_title()
        {
            var downloadId = Guid.NewGuid().ToString().ToUpper();

            GivenHistoryItem(downloadId, TITLE, _mp3, EntityHistoryEventType.Grabbed);
            GivenHistoryItem(downloadId, TITLE, _flac, EntityHistoryEventType.ComicFileImported);

            _remoteIssue.Release = Builder<TorrentInfo>.CreateNew()
                                                         .With(t => t.DownloadProtocol = DownloadProtocol.Torrent)
                                                         .With(t => t.InfoHash = downloadId)
                                                         .Build();

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }
    }
}
