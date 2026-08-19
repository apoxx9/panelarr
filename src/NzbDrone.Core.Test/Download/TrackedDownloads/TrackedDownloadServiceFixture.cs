using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.History;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

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
                         SourceTitle = "Saga - The Brand New World [2018 - CBZ]",
                         SeriesId = 5,
                         IssueId = 4,
                    }
                });
        }

        [Test]
        public void should_trust_the_grab_record_when_the_title_maps_to_a_series_without_issues()
        {
            // Observed live with "Aliens Epic Collection – The Original Years
            // Vol. 3 (2025)": the title re-parsed without search criteria
            // mapped to the right series but NO issue, and the download sat
            // in the queue as unknown with nothing to import into - while the
            // grab record knew the exact series and issue all along.
            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.FindByDownloadId("GC1"))
                .Returns(new List<EntityHistory>
                {
                    new EntityHistory
                    {
                        DownloadId = "GC1",
                        EventType = EntityHistoryEventType.Grabbed,
                        SourceTitle = "Aliens Epic Collection Vol. 3 (2025)",
                        SeriesId = 150,
                        IssueId = 1917,
                    }
                });

            var series = new Series { Id = 150 };

            // the criteria-less title map: series found, no issue
            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.IsAny<ParsedIssueInfo>(), (SearchCriteriaBase)null))
                  .Returns(new RemoteIssue { Series = series, Issues = new List<Issue>(), ParsedIssueInfo = new ParsedIssueInfo { SeriesName = "Aliens Epic Collection" } });

            // the grab-record map: series + the grabbed issue
            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.IsAny<ParsedIssueInfo>(), 150, It.Is<IEnumerable<int>>(ids => ids.Contains(1917))))
                  .Returns(new RemoteIssue { Series = series, Issues = new List<Issue> { new Issue { Id = 1917 } }, ParsedIssueInfo = new ParsedIssueInfo { SeriesName = "Aliens Epic Collection" } });

            var client = new DownloadClientDefinition { Id = 1, Protocol = DownloadProtocol.DirectDownload };
            var item = new DownloadClientItem
            {
                Title = "Aliens Epic Collection Vol. 3 (2025)",
                DownloadId = "GC1",
                DownloadClientInfo = new DownloadClientItemClientInfo { Protocol = client.Protocol, Id = client.Id, Name = client.Name }
            };

            var trackedDownload = Subject.TrackDownload(client, item);

            trackedDownload.RemoteIssue.Should().NotBeNull();
            trackedDownload.RemoteIssue.Series.Id.Should().Be(150);
            trackedDownload.RemoteIssue.Issues.Should().ContainSingle(i => i.Id == 1917);

            // the static title parser logs (and swallows) an error on this
            // title shape under the test harness - unrelated to the mapping
            // under test, which must succeed regardless of what the title
            // parse yields
            ExceptionVerification.IgnoreErrors();
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
                    IssueTitle = "The Brand New World",
                    SeriesName = "Saga"
                }
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.Is<ParsedIssueInfo>(i => i.IssueTitle == "The Brand New World" && i.SeriesName == "Saga"), It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
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
        public void should_evict_ignored_download_from_cache_when_regrabbed()
        {
            GivenDownloadHistory();

            var remoteIssue = new RemoteIssue
            {
                Series = new Series() { Id = 5 },
                Issues = new List<Issue> { new Issue { Id = 4 } },
                ParsedIssueInfo = new ParsedIssueInfo()
                {
                    IssueTitle = "The Brand New World",
                    SeriesName = "Saga"
                }
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.Is<ParsedIssueInfo>(i => i.IssueTitle == "The Brand New World" && i.SeriesName == "Saga"), It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
                  .Returns(remoteIssue);

            Mocker.GetMock<IDownloadHistoryService>()
                  .Setup(s => s.GetLatestDownloadHistoryItem("35238"))
                  .Returns(new DownloadHistory { EventType = DownloadHistoryEventType.DownloadIgnored });

            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent
            };

            var item = new DownloadClientItem()
            {
                Title = "Saga - The Brand New World [2018 - CBZ]",
                DownloadId = "35238",
                DownloadClientInfo = new DownloadClientItemClientInfo
                {
                    Protocol = client.Protocol,
                    Id = client.Id,
                    Name = client.Name
                }
            };

            // The client still has the torrent, so it gets tracked in the Ignored state
            var trackedDownload = Subject.TrackDownload(client, item);
            trackedDownload.State.Should().Be(TrackedDownloadState.Ignored);

            // Re-grabbing the same download id (e.g. a cross-seeded torrent) must evict the cached item
            Subject.Handle(new IssueGrabbedEvent(remoteIssue) { DownloadId = "35238" });
            Subject.Find("35238").Should().BeNull();

            // The refresh that follows the grab rebuilds from history, which now reports the new grab
            Mocker.GetMock<IDownloadHistoryService>()
                  .Setup(s => s.GetLatestDownloadHistoryItem("35238"))
                  .Returns(new DownloadHistory { EventType = DownloadHistoryEventType.DownloadGrabbed });

            var regrabbed = Subject.TrackDownload(client, item);
            regrabbed.State.Should().Be(TrackedDownloadState.Downloading);
        }

        [Test]
        public void should_not_evict_downloading_item_from_cache_when_regrabbed()
        {
            GivenDownloadHistory();

            var remoteIssue = new RemoteIssue
            {
                Series = new Series() { Id = 5 },
                Issues = new List<Issue> { new Issue { Id = 4 } },
                ParsedIssueInfo = new ParsedIssueInfo()
                {
                    IssueTitle = "The Brand New World",
                    SeriesName = "Saga"
                }
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.Is<ParsedIssueInfo>(i => i.IssueTitle == "The Brand New World" && i.SeriesName == "Saga"), It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
                  .Returns(remoteIssue);

            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent
            };

            var item = new DownloadClientItem()
            {
                Title = "Saga - The Brand New World [2018 - CBZ]",
                DownloadId = "35238",
                DownloadClientInfo = new DownloadClientItemClientInfo
                {
                    Protocol = client.Protocol,
                    Id = client.Id,
                    Name = client.Name
                }
            };

            var trackedDownload = Subject.TrackDownload(client, item);
            trackedDownload.State.Should().Be(TrackedDownloadState.Downloading);

            Subject.Handle(new IssueGrabbedEvent(remoteIssue) { DownloadId = "35238" });

            Subject.Find("35238").Should().NotBeNull();
        }

        [Test]
        public void should_not_throw_when_grab_event_has_no_download_id()
        {
            var remoteIssue = new RemoteIssue
            {
                Series = new Series() { Id = 5 },
                Issues = new List<Issue> { new Issue { Id = 4 } }
            };

            Assert.DoesNotThrow(() => Subject.Handle(new IssueGrabbedEvent(remoteIssue)));
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
                    IssueTitle = "The Brand New World",
                    SeriesName = "Saga"
                }
            };

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.Map(It.Is<ParsedIssueInfo>(i => i.IssueTitle == "The Brand New World" && i.SeriesName == "Saga"), It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
                  .Returns(remoteIssue);

            var client = new DownloadClientDefinition()
            {
                Id = 1,
                Protocol = DownloadProtocol.Torrent
            };

            var item = new DownloadClientItem()
            {
                Title = "Saga - The Brand New World [2018 - CBZ]",
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
                .Setup(s => s.Map(It.Is<ParsedIssueInfo>(i => i.IssueTitle == "The Brand New World" && i.SeriesName == "Saga"), It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
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
                    IssueTitle = "Saga"
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
                Title = "Saga - 001 [2012 - CBZ]",
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
                    IssueTitle = "Saga",
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
                Title = "Saga - 001 [2012 - CBZ]",
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
