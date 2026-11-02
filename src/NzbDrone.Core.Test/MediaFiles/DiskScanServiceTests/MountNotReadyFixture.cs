using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.DiskScanServiceTests
{
    /// <summary>
    /// The library share is mounted after Docker starts on the host this runs
    /// on, so every reboot leaves a window where a root folder exists and is
    /// empty. That is "not mounted yet", not "the library was deleted".
    /// </summary>
    [TestFixture]
    public class MountNotReadyFixture : CoreTest<DiskScanService>
    {
        private string _rootFolder;

        [SetUp]
        public void Setup()
        {
            _rootFolder = @"C:\Test\Comics".AsOsAgnostic();

            Mocker.GetMock<IRootFolderService>()
                .Setup(s => s.GetBestRootFolder(It.IsAny<string>()))
                .Returns(new RootFolder { Path = _rootFolder });

            Mocker.GetMock<IMediaFileService>()
                .Setup(v => v.GetFilesWithBasePath(It.IsAny<string>()))
                .Returns(new List<ComicFile>());

            // No test should sit through the real back-off
            Subject.MountRetryBackOff = new[] { TimeSpan.Zero, TimeSpan.Zero };
        }

        private void GivenKnownFiles()
        {
            Mocker.GetMock<IMediaFileService>()
                .Setup(v => v.GetFilesWithBasePath(_rootFolder))
                .Returns(new List<ComicFile> { new ComicFile { Path = Path.Combine(_rootFolder, "Saga 001.cbz") } });
        }

        private void GivenFolderEmpty(params bool[] answers)
        {
            var sequence = Mocker.GetMock<IDiskProvider>()
                                 .SetupSequence(s => s.FolderEmpty(_rootFolder));

            foreach (var answer in answers)
            {
                sequence = sequence.Returns(answer);
            }
        }

        [Test]
        public void should_wait_and_succeed_when_the_mount_arrives()
        {
            GivenKnownFiles();
            GivenFolderEmpty(true, true, false);

            Subject.WaitForRootFolderContent(_rootFolder).Should().BeTrue();

            // One warning for the episode, then an Info once it recovers
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_give_up_after_the_back_off_is_exhausted()
        {
            GivenKnownFiles();
            GivenFolderEmpty(true, true, true);

            Subject.WaitForRootFolderContent(_rootFolder).Should().BeFalse();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_wait_at_all_for_a_genuinely_empty_library()
        {
            // No file records, so an empty root is simply empty
            GivenFolderEmpty(true);

            Subject.WaitForRootFolderContent(_rootFolder).Should().BeFalse();

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.FolderEmpty(_rootFolder), Times.Once());
        }

        [Test]
        public void should_return_immediately_when_the_folder_has_content()
        {
            GivenKnownFiles();
            GivenFolderEmpty(false);

            Subject.WaitForRootFolderContent(_rootFolder).Should().BeTrue();

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.FolderEmpty(_rootFolder), Times.Once());
        }

        [Test]
        public void should_warn_once_per_episode_not_once_per_scan()
        {
            // It used to warn on every scan of every series, burying the one
            // line that correlates the incident with a host reboot
            GivenKnownFiles();
            Mocker.GetMock<IDiskProvider>().Setup(s => s.FolderEmpty(_rootFolder)).Returns(true);

            Subject.WaitForRootFolderContent(_rootFolder).Should().BeFalse();
            Subject.WaitForRootFolderContent(_rootFolder).Should().BeFalse();
            Subject.WaitForRootFolderContent(_rootFolder).Should().BeFalse();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_warn_again_after_a_recovery()
        {
            GivenKnownFiles();
            GivenFolderEmpty(true, true, true, false, true, true, true);

            Subject.WaitForRootFolderContent(_rootFolder).Should().BeFalse();
            Subject.WaitForRootFolderContent(_rootFolder).Should().BeTrue();
            Subject.WaitForRootFolderContent(_rootFolder).Should().BeFalse();

            // A fresh episode deserves its own warning
            ExceptionVerification.ExpectedWarns(2);
        }
    }
}
