using System.Collections.Generic;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download
{
    [TestFixture]
    public class RedownloadFailedDownloadServiceFixture : CoreTest<RedownloadFailedDownloadService>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(x => x.AutoRedownloadFailed)
                .Returns(true);

            Mocker.GetMock<IIssueService>()
                .Setup(x => x.GetIssuesBySeries(It.IsAny<int>()))
                .Returns(Builder<Issue>.CreateListOfSize(3).Build() as List<Issue>);
        }

        [Test]
        public void should_skip_redownload_if_event_has_skipredownload_set()
        {
            var failedEvent = new DownloadFailedEvent
            {
                SeriesId = 1,
                IssueIds = new List<int> { 1 },
                SkipRedownload = true
            };

            Subject.Handle(failedEvent);

            Mocker.GetMock<IManageCommandQueue>()
                .Verify(x => x.Push(It.IsAny<Command>(), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()),
                        Times.Never());
        }

        [Test]
        public void should_skip_redownload_if_redownload_failed_disabled()
        {
            var failedEvent = new DownloadFailedEvent
            {
                SeriesId = 1,
                IssueIds = new List<int> { 1 }
            };

            Mocker.GetMock<IConfigService>()
                .Setup(x => x.AutoRedownloadFailed)
                .Returns(false);

            Subject.Handle(failedEvent);

            Mocker.GetMock<IManageCommandQueue>()
                .Verify(x => x.Push(It.IsAny<Command>(), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()),
                        Times.Never());
        }

        [Test]
        public void should_redownload_book_on_failure()
        {
            var failedEvent = new DownloadFailedEvent
            {
                SeriesId = 1,
                IssueIds = new List<int> { 2 }
            };

            Subject.Handle(failedEvent);

            Mocker.GetMock<IManageCommandQueue>()
                .Verify(x => x.Push(It.Is<IssueSearchCommand>(c => c.IssueIds.Count == 1 &&
                                                              c.IssueIds[0] == 2),
                                    It.IsAny<CommandPriority>(),
                                    It.IsAny<CommandTrigger>()),
                        Times.Once());

            Mocker.GetMock<IManageCommandQueue>()
                .Verify(x => x.Push(It.IsAny<SeriesSearchCommand>(), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()),
                        Times.Never());
        }

        [Test]
        public void should_redownload_multiple_books_on_failure()
        {
            var failedEvent = new DownloadFailedEvent
            {
                SeriesId = 1,
                IssueIds = new List<int> { 2, 3 }
            };

            Subject.Handle(failedEvent);

            Mocker.GetMock<IManageCommandQueue>()
                .Verify(x => x.Push(It.Is<IssueSearchCommand>(c => c.IssueIds.Count == 2 &&
                                                              c.IssueIds[0] == 2 &&
                                                              c.IssueIds[1] == 3),
                                    It.IsAny<CommandPriority>(),
                                    It.IsAny<CommandTrigger>()),
                        Times.Once());

            Mocker.GetMock<IManageCommandQueue>()
                .Verify(x => x.Push(It.IsAny<SeriesSearchCommand>(), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()),
                        Times.Never());
        }

        [Test]
        public void should_redownload_author_on_failure()
        {
            // note that series is set to have 3 issues in setup
            var failedEvent = new DownloadFailedEvent
            {
                SeriesId = 2,
                IssueIds = new List<int> { 1, 2, 3 }
            };

            Subject.Handle(failedEvent);

            Mocker.GetMock<IManageCommandQueue>()
                .Verify(x => x.Push(It.Is<SeriesSearchCommand>(c => c.SeriesId == failedEvent.SeriesId),
                                    It.IsAny<CommandPriority>(),
                                    It.IsAny<CommandTrigger>()),
                        Times.Once());

            Mocker.GetMock<IManageCommandQueue>()
                .Verify(x => x.Push(It.IsAny<IssueSearchCommand>(), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()),
                        Times.Never());
        }
    }
}
