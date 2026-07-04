using System;
using System.IO;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.TrackFileMovingServiceTests
{
    [TestFixture]
    public class MoveComicFileFixture : CoreTest<ComicFileMovingService>
    {
        private Series _series;
        private ComicFile _trackFile;
        private LocalIssue _localtrack;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>.CreateNew()
                                     .With(s => s.Path = @"C:\Test\Comics\Saga (2012)".AsOsAgnostic())
                                     .Build();

            _trackFile = Builder<ComicFile>.CreateNew()
                                               .With(f => f.Path = null)
                                               .With(f => f.Path = Path.Combine(_series.Path, @"Issue\File.cbz"))
                                               .Build();

            _localtrack = Builder<LocalIssue>.CreateNew()
                                                 .With(l => l.Series = _series)
                                                 .With(l => l.Issue = Builder<Issue>.CreateNew().Build())
                                                 .Build();

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildComicFileName(It.IsAny<Series>(), It.IsAny<Issue>(), It.IsAny<ComicFile>(), null, null))
                  .Returns("File Name");

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildComicFilePath(It.IsAny<Series>(), It.IsAny<Issue>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(@"C:\Test\Comics\Saga (2012)\Issue\File Name.cbz".AsOsAgnostic());

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildIssuePath(It.IsAny<Series>()))
                  .Returns(@"C:\Test\Comics\Saga (2012)\Issue".AsOsAgnostic());

            var rootFolder = @"C:\Test\Comics\".AsOsAgnostic();
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(rootFolder))
                  .Returns(true);

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>()))
                  .Returns(rootFolder);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FileExists(It.IsAny<string>()))
                  .Returns(true);
        }

        [Test]
        public void should_catch_UnauthorizedAccessException_during_folder_inheritance()
        {
            WindowsOnly();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.InheritFolderPermissions(It.IsAny<string>()))
                  .Throws<UnauthorizedAccessException>();

            Subject.MoveComicFile(_trackFile, _localtrack);
        }

        [Test]
        public void should_catch_InvalidOperationException_during_folder_inheritance()
        {
            WindowsOnly();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.InheritFolderPermissions(It.IsAny<string>()))
                  .Throws<InvalidOperationException>();

            Subject.MoveComicFile(_trackFile, _localtrack);
        }

        [Test]
        public void should_notify_on_series_folder_creation()
        {
            Subject.MoveComicFile(_trackFile, _localtrack);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent<IssueFolderCreatedEvent>(It.Is<IssueFolderCreatedEvent>(p =>
                      p.SeriesFolder.IsNotNullOrWhiteSpace())), Times.Once());
        }

        [Test]
        public void should_notify_on_book_folder_creation()
        {
            Subject.MoveComicFile(_trackFile, _localtrack);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent<IssueFolderCreatedEvent>(It.Is<IssueFolderCreatedEvent>(p =>
                      p.IssueFolder.IsNotNullOrWhiteSpace())), Times.Once());
        }

        [Test]
        public void should_move_when_series_is_under_missing_publisher_folder_and_root_exists()
        {
            var rootFolder = @"C:\Test\Comics\".AsOsAgnostic();
            _series.Path = @"C:\Test\Comics\Dynamite Entertainment\Ben 10 (2026)".AsOsAgnostic();

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildComicFilePath(It.IsAny<Series>(), It.IsAny<Issue>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Returns(Path.Combine(_series.Path, "File Name.cbz"));

            Mocker.GetMock<IBuildFileNames>()
                  .Setup(s => s.BuildIssuePath(It.IsAny<Series>()))
                  .Returns(_series.Path);

            // Publisher folder does not exist; only the configured root folder does.
            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolderPath(_series.Path))
                  .Returns(rootFolder);

            Subject.MoveComicFile(_trackFile, _localtrack);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent<IssueFolderCreatedEvent>(It.Is<IssueFolderCreatedEvent>(p =>
                      p.SeriesFolder.IsNotNullOrWhiteSpace())), Times.Once());
        }

        [Test]
        public void should_throw_when_root_folder_does_not_exist()
        {
            var rootFolder = @"C:\Test\Comics\".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(rootFolder))
                  .Returns(false);

            Assert.Throws<RootFolderNotFoundException>(() => Subject.MoveComicFile(_trackFile, _localtrack));
        }

        [Test]
        public void should_not_notify_if_series_folder_already_exists()
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(_series.Path))
                  .Returns(true);

            Subject.MoveComicFile(_trackFile, _localtrack);

            Mocker.GetMock<IEventAggregator>()
                  .Verify(s => s.PublishEvent<IssueFolderCreatedEvent>(It.Is<IssueFolderCreatedEvent>(p =>
                      p.SeriesFolder.IsNotNullOrWhiteSpace())), Times.Never());
        }
    }
}
