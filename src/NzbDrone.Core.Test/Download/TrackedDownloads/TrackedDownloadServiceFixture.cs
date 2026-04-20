using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.TrackedDownloads
{
    [TestFixture]
    public class TrackedDownloadServiceFixture : CoreTest<TrackedDownloadService>
    {
        private void GivenDownloadHistory()
        {
            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.FindByDownloadId(It.Is<string>(sr => sr == "35238")))
                .Returns(new List<EntityHistory>()
                {
                    new EntityHistory()
                    {
                         DownloadId = "35238",
                         SourceTitle = "Audio Series - Audio Issue [2018 - FLAC]",
                         SeriesId = 5,
                         IssueId = 4,
                    }
                });
        }

        [Test]
        public void should_track_downloads_using_the_source_title_if_it_cannot_be_found_using_the_download_title()
        {
            GivenDownloadHistory();

            var remoteIssue = new RemoteIssue
            {
                Series = new Series() { Id = 5 },
                Issues = new List<Issue> { new Issue { Id = 4 } },
                ParsedIssueInfo = new ParsedIssueInfo()
                {
                    IssueTitle = "Audio Issue",
                    SeriesName = "Audio Series"
                }
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.Is<ParsedIssueInfo>(i => i.IssueTitle == "Audio Issue" && i.SeriesName == "Audio Series"), It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
                  .Returns(remoteIssue);

            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent
            };

            var item = new DownloadClientItem()
            {
                Title = "The torrent release folder",
                DownloadId = "35238",
                DownloadClientInfo = new DownloadClientItemClientInfo
                {
                    Protocol = client.Protocol,
                    Id = client.Id,
                    Name = client.Name
                }
            };

            var trackedDownload = Subject.TrackDownload(client, item);

            trackedDownload.Should().NotBeNull();
            trackedDownload.RemoteIssue.Should().NotBeNull();
            trackedDownload.RemoteIssue.Series.Should().NotBeNull();
            trackedDownload.RemoteIssue.Series.Id.Should().Be(5);
            trackedDownload.RemoteIssue.Issues.First().Id.Should().Be(4);
        }

        [Test]
        public void should_unmap_tracked_download_if_book_deleted()
        {
            GivenDownloadHistory();

            var remoteIssue = new RemoteIssue
            {
                Series = new Series() { Id = 5 },
                Issues = new List<Issue> { new Issue { Id = 4 } },
                ParsedIssueInfo = new ParsedIssueInfo()
                {
                    IssueTitle = "Audio Issue",
                    SeriesName = "Audio Series"
                }
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.Is<ParsedIssueInfo>(i => i.IssueTitle == "Audio Issue" && i.SeriesName == "Audio Series"), It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
                  .Returns(remoteIssue);

            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent
            };

            var item = new DownloadClientItem()
            {
                Title = "Audio Series - Audio Issue [2018 - FLAC]",
                DownloadId = "35238",
                DownloadClientInfo = new DownloadClientItemClientInfo
                {
                    Protocol = client.Protocol,
                    Id = client.Id,
                    Name = client.Name
                }
            };

            // get a tracked download in place
            var trackedDownload = Subject.TrackDownload(client, item);
            Subject.GetTrackedDownloads().Should().HaveCount(1);

            // simulate deletion - issue no longer maps
            Mocker.GetMock<IParsingService>()
                .Setup(s => s.Map(It.Is<ParsedIssueInfo>(i => i.IssueTitle == "Audio Issue" && i.SeriesName == "Audio Series"), It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
                .Returns(default(RemoteIssue));

            // handle deletion event
            Subject.Handle(new IssueInfoRefreshedEvent(remoteIssue.Series, new List<Issue>(), new List<Issue>(), remoteIssue.Issues));

            // verify download has null remote issue
            var trackedDownloads = Subject.GetTrackedDownloads();
            trackedDownloads.Should().HaveCount(1);
            trackedDownloads.First().RemoteIssue.Should().BeNull();
        }

        [Test]
        public void should_not_throw_when_processing_deleted_episodes()
        {
            GivenDownloadHistory();

            var remoteEpisode = new RemoteIssue
            {
                Series = new Series() { Id = 5 },
                Issues = new List<Issue> { new Issue { Id = 4 } },
                ParsedIssueInfo = new ParsedIssueInfo()
                {
                    IssueTitle = "TV SeriesGroup"
                }
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.IsAny<ParsedIssueInfo>(), It.IsAny<int>(), It.IsAny<List<int>>()))
                  .Returns(default(RemoteIssue));

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                  .Returns(new List<EntityHistory>());

            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent
            };

            var item = new DownloadClientItem()
            {
                Title = "TV SeriesGroup - S01E01",
                DownloadId = "12345",
                DownloadClientInfo = new DownloadClientItemClientInfo
                {
                    Id = 1,
                    Type = "Blackhole",
                    Name = "Blackhole Client",
                    Protocol = DownloadProtocol.Torrent
                }
            };

            Subject.TrackDownload(client, item);
            Subject.GetTrackedDownloads().Should().HaveCount(1);

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.IsAny<ParsedIssueInfo>(), It.IsAny<int>(), It.IsAny<List<int>>()))
                  .Returns(default(RemoteIssue));

            Subject.Handle(new IssueInfoRefreshedEvent(remoteEpisode.Series, new List<Issue>(), new List<Issue>(), remoteEpisode.Issues));

            var trackedDownloads = Subject.GetTrackedDownloads();
            trackedDownloads.Should().HaveCount(1);
            trackedDownloads.First().RemoteIssue.Should().BeNull();
        }

        [Test]
        public void should_not_throw_when_processing_deleted_series()
        {
            GivenDownloadHistory();

            var remoteEpisode = new RemoteIssue
            {
                Series = new Series() { Id = 5 },
                Issues = new List<Issue> { new Issue { Id = 4 } },
                ParsedIssueInfo = new ParsedIssueInfo()
                {
                    IssueTitle = "TV SeriesGroup",
                }
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.IsAny<ParsedIssueInfo>(), It.IsAny<int>(), It.IsAny<List<int>>()))
                  .Returns(default(RemoteIssue));

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                  .Returns(new List<EntityHistory>());

            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent
            };

            var item = new DownloadClientItem()
            {
                Title = "TV SeriesGroup - S01E01",
                DownloadId = "12345",
                DownloadClientInfo = new DownloadClientItemClientInfo
                {
                    Id = 1,
                    Type = "Blackhole",
                    Name = "Blackhole Client",
                    Protocol = DownloadProtocol.Torrent
                }
            };

            Subject.TrackDownload(client, item);
            Subject.GetTrackedDownloads().Should().HaveCount(1);

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.IsAny<ParsedIssueInfo>(), It.IsAny<int>(), It.IsAny<List<int>>()))
                  .Returns(default(RemoteIssue));

            Subject.Handle(new SeriesDeletedEvent(remoteEpisode.Series, true, true));

            var trackedDownloads = Subject.GetTrackedDownloads();
            trackedDownloads.Should().HaveCount(1);
            trackedDownloads.First().RemoteIssue.Should().BeNull();
        }
    }
}
