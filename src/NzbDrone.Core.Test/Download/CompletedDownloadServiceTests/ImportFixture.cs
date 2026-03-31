using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download.CompletedDownloadServiceTests
{
    [TestFixture]
    public class ImportFixture : CoreTest<CompletedDownloadService>
    {
        private TrackedDownload _trackedDownload;
        private Series _series;

        [SetUp]
        public void Setup()
        {
            var completed = Builder<DownloadClientItem>.CreateNew()
                                                    .With(h => h.Status = DownloadItemStatus.Completed)
                                                    .With(h => h.OutputPath = new OsPath(@"C:\DropFolder\MyDownload".AsOsAgnostic()))
                                                    .With(h => h.Title = "Drone.S01E01.HDTV")
                                                    .Build();

            var remoteIssue = BuildRemoteIssue();

            _trackedDownload = Builder<TrackedDownload>.CreateNew()
                    .With(c => c.State = TrackedDownloadState.Downloading)
                    .With(c => c.DownloadItem = completed)
                    .With(c => c.RemoteIssue = remoteIssue)
                    .Build();

            _series = Builder<Series>.CreateNew()
                .Build();

            Mocker.GetMock<IDownloadClient>()
              .SetupGet(c => c.Definition)
              .Returns(new DownloadClientDefinition { Id = 1, Name = "testClient" });

            Mocker.GetMock<IProvideDownloadClient>()
                  .Setup(c => c.Get(It.IsAny<int>()))
                  .Returns(Mocker.GetMock<IDownloadClient>().Object);

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.MostRecentForDownloadId(_trackedDownload.DownloadItem.DownloadId))
                  .Returns(new EntityHistory());

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetSeries("Drone.S01E01.HDTV"))
                  .Returns(remoteIssue.Series);

            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                .Returns(new List<EntityHistory>());

            Mocker.GetMock<IProvideImportItemService>()
                .Setup(s => s.ProvideImportItem(It.IsAny<DownloadClientItem>(), It.IsAny<DownloadClientItem>()))
                .Returns<DownloadClientItem, DownloadClientItem>((i, p) => i);
        }

        private Issue CreateIssue(int id)
        {
            return new Issue
            {
                Id = id
            };
        }

        private RemoteIssue BuildRemoteIssue()
        {
            return new RemoteIssue
            {
                Series = new Series(),
                Issues = new List<Issue> { CreateIssue(1) }
            };
        }

        private void GivenABadlyNamedDownload()
        {
            _trackedDownload.DownloadItem.DownloadId = "1234";
            _trackedDownload.DownloadItem.Title = "Droned Pilot"; // Set a badly named download
            Mocker.GetMock<IHistoryService>()
               .Setup(s => s.MostRecentForDownloadId(It.Is<string>(i => i == "1234")))
               .Returns(new EntityHistory() { SourceTitle = "Droned S01E01" });

            Mocker.GetMock<IParsingService>()
               .Setup(s => s.GetSeries(It.IsAny<string>()))
               .Returns((Series)null);

            Mocker.GetMock<IParsingService>()
                .Setup(s => s.GetSeries("Droned S01E01"))
                .Returns(BuildRemoteIssue().Series);
        }

        private void GivenSeriesMatch()
        {
            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetSeries(It.IsAny<string>()))
                  .Returns(_trackedDownload.RemoteIssue.Series);
        }

        [Test]
        public void should_not_mark_as_imported_if_all_files_were_rejected()
        {
            Mocker.GetMock<IDownloadedIssuesImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Series>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
                           {
                               new ImportResult(
                                   new ImportDecision<LocalIssue>(
                                       new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic() }, new Rejection("Rejected!")), "Test Failure"),

                               new ImportResult(
                                   new ImportDecision<LocalIssue>(
                                       new LocalIssue { Path = @"C:\TestPath\Droned.S01E02.mkv".AsOsAgnostic() }, new Rejection("Rejected!")), "Test Failure")
                           });

            Subject.Import(_trackedDownload);

            Mocker.GetMock<IEventAggregator>()
                .Verify(v => v.PublishEvent<DownloadCompletedEvent>(It.IsAny<DownloadCompletedEvent>()), Times.Never());

            AssertNotImported();
        }

        [Test]
        public void should_not_mark_as_imported_if_no_tracks_were_parsed()
        {
            Mocker.GetMock<IDownloadedIssuesImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Series>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
                           {
                               new ImportResult(
                                   new ImportDecision<LocalIssue>(
                                       new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic() }, new Rejection("Rejected!")), "Test Failure"),

                               new ImportResult(
                                   new ImportDecision<LocalIssue>(
                                       new LocalIssue { Path = @"C:\TestPath\Droned.S01E02.mkv".AsOsAgnostic() }, new Rejection("Rejected!")), "Test Failure")
                           });

            _trackedDownload.RemoteIssue.Issues.Clear();

            Subject.Import(_trackedDownload);

            AssertNotImported();
        }

        [Test]
        public void should_not_mark_as_failed_if_nothing_found_to_import()
        {
            Mocker.GetMock<IDownloadedIssuesImportService>()
                .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Series>(), It.IsAny<DownloadClientItem>()))
                .Returns(new List<ImportResult>());

            Subject.Import(_trackedDownload);

            _trackedDownload.State.Should().Be(TrackedDownloadState.ImportPending);
        }

        [Test]
        public void should_not_mark_as_imported_if_all_files_were_skipped()
        {
            Mocker.GetMock<IDownloadedIssuesImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Series>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
                           {
                               new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic() }), "Test Failure"),
                               new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic() }), "Test Failure")
                           });

            Subject.Import(_trackedDownload);

            AssertNotImported();
        }

        [Test]
        public void should_mark_as_imported_if_all_tracks_were_imported_but_extra_files_were_not()
        {
            GivenSeriesMatch();

            _trackedDownload.RemoteIssue.Issues = new List<Issue>
            {
                CreateIssue(1)
            };

            Mocker.GetMock<IDownloadedIssuesImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Series>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
                           {
                               new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic(), Series = _series })),
                               new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic(), Series = _series }), "Test Failure")
                           });

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                  .Returns(new List<EntityHistory>());

            Subject.Import(_trackedDownload);

            AssertImported();
        }

        [Test]
        public void should_not_mark_as_imported_if_some_tracks_were_not_imported()
        {
            _trackedDownload.RemoteIssue.Issues = new List<Issue>
            {
                CreateIssue(1),
                CreateIssue(1),
                CreateIssue(1)
            };

            Mocker.GetMock<IDownloadedIssuesImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Series>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
                           {
                               new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic() })),
                               new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic() })),
                               new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic() }), "Test Failure"),
                               new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic() }), "Test Failure"),
                               new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic() }), "Test Failure")
                           });

            var history = Builder<EntityHistory>.CreateListOfSize(2)
                                                  .BuildList();

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                  .Returns(history);

            Mocker.GetMock<ITrackedDownloadAlreadyImported>()
                  .Setup(s => s.IsImported(_trackedDownload, history))
                  .Returns(true);

            Subject.Import(_trackedDownload);

            AssertNotImported();
        }

        [Test]
        public void should_not_mark_as_imported_if_some_of_episodes_were_not_imported_including_history()
        {
            var issues = Builder<Issue>.CreateListOfSize(3).BuildList();

            _trackedDownload.RemoteIssue.Issues = issues;

            Mocker.GetMock<IDownloadedIssuesImportService>()
                .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Series>(), It.IsAny<DownloadClientItem>()))
                .Returns(new List<ImportResult>
                {
                    new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv" })),
                    new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv" }), "Test Failure"),
                    new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv" }), "Test Failure")
                });

            var history = Builder<EntityHistory>.CreateListOfSize(2)
                                                  .BuildList();

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                  .Returns(history);

            Mocker.GetMock<ITrackedDownloadAlreadyImported>()
                  .Setup(s => s.IsImported(It.IsAny<TrackedDownload>(), It.IsAny<List<EntityHistory>>()))
                  .Returns(false);

            Subject.Import(_trackedDownload);

            AssertNotImported();
        }

        [Test]
        public void should_mark_as_imported_if_all_tracks_were_imported()
        {
            _trackedDownload.RemoteIssue.Issues = new List<Issue>
            {
                CreateIssue(1)
            };

            Mocker.GetMock<IDownloadedIssuesImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Series>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
                           {
                               new ImportResult(
                                   new ImportDecision<LocalIssue>(
                                       new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic(), Series = _series })),

                               new ImportResult(
                                   new ImportDecision<LocalIssue>(
                                       new LocalIssue { Path = @"C:\TestPath\Droned.S01E02.mkv".AsOsAgnostic(), Series = _series }))
                           });

            Subject.Import(_trackedDownload);

            AssertImported();
        }

        [Test]
        public void should_mark_as_imported_if_all_episodes_were_imported_including_history()
        {
            var issues = Builder<Issue>.CreateListOfSize(2).BuildList();

            _trackedDownload.RemoteIssue.Issues = issues;

            Mocker.GetMock<IDownloadedIssuesImportService>()
                .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Series>(), It.IsAny<DownloadClientItem>()))
                .Returns(new List<ImportResult>
                {
                    new ImportResult(
                        new ImportDecision<LocalIssue>(
                            new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv", Issue = issues[0], Series = _series })),

                    new ImportResult(
                        new ImportDecision<LocalIssue>(
                            new LocalIssue { Path = @"C:\TestPath\Droned.S01E02.mkv", Issue = issues[1], Series = _series }), "Test Failure")
                });

            var history = Builder<EntityHistory>.CreateListOfSize(2)
                .All()
                .With(x => x.EventType = EntityHistoryEventType.ComicFileImported)
                .With(x => x.SeriesId = 1)
                .BuildList();

            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.FindByDownloadId(It.IsAny<string>()))
                  .Returns(history);

            Mocker.GetMock<ITrackedDownloadAlreadyImported>()
                  .Setup(s => s.IsImported(It.IsAny<TrackedDownload>(), It.IsAny<List<EntityHistory>>()))
                  .Returns(true);

            Subject.Import(_trackedDownload);

            AssertImported();
        }

        [Test]
        public void should_mark_as_imported_if_the_download_can_be_tracked_using_the_source_seriesid()
        {
            GivenABadlyNamedDownload();

            Mocker.GetMock<IDownloadedIssuesImportService>()
                  .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Series>(), It.IsAny<DownloadClientItem>()))
                  .Returns(new List<ImportResult>
                           {
                               new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = @"C:\TestPath\Droned.S01E01.mkv".AsOsAgnostic(), Series = _series }))
                           });

            Subject.Import(_trackedDownload);

            AssertImported();
        }

        private void AssertNotImported()
        {
            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<DownloadCompletedEvent>()), Times.Never());

            _trackedDownload.State.Should().Be(TrackedDownloadState.ImportFailed);
        }

        private void AssertImported()
        {
            Mocker.GetMock<IDownloadedIssuesImportService>()
                .Verify(v => v.ProcessPath(_trackedDownload.DownloadItem.OutputPath.FullPath, ImportMode.Auto, _trackedDownload.RemoteIssue.Series, _trackedDownload.DownloadItem), Times.Once());

            Mocker.GetMock<IEventAggregator>()
                  .Verify(v => v.PublishEvent(It.IsAny<DownloadCompletedEvent>()), Times.Once());

            _trackedDownload.State.Should().Be(TrackedDownloadState.Imported);
        }
    }
}
