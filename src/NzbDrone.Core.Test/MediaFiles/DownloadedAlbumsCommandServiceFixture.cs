using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class DownloadedIssuesCommandServiceFixture : FileSystemTest<DownloadedIssuesCommandService>
    {
        private string _downloadFolder = "c:\\drop_other\\Show.S01E01\\".AsOsAgnostic();
        private string _downloadFile = "c:\\drop_other\\Show.S01E01.mkv".AsOsAgnostic();

        private TrackedDownload _trackedDownload;

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IDownloadedIssuesImportService>()
                .Setup(v => v.ProcessRootFolder(It.IsAny<IDirectoryInfo>()))
                .Returns(new List<ImportResult>());

            Mocker.GetMock<IDownloadedIssuesImportService>()
                .Setup(v => v.ProcessPath(It.IsAny<string>(), It.IsAny<ImportMode>(), It.IsAny<Series>(), It.IsAny<DownloadClientItem>()))
                .Returns(new List<ImportResult>());

            var downloadItem = Builder<DownloadClientItem>.CreateNew()
                .With(v => v.DownloadId = "sab1")
                .With(v => v.Status = DownloadItemStatus.Downloading)
                .Build();

            var remoteIssue = Builder<RemoteIssue>.CreateNew()
                .With(v => v.Series = new Series())
                .Build();

            _trackedDownload = new TrackedDownload
            {
                DownloadItem = downloadItem,
                RemoteIssue = remoteIssue,
                State = TrackedDownloadState.Downloading
            };
        }

        private void GivenExistingFolder(string path)
        {
            FileSystem.AddDirectory(path);
        }

        private void GivenExistingFile(string path)
        {
            FileSystem.AddFile(path, new MockFileData(string.Empty));
        }

        private void GivenValidQueueItem()
        {
            Mocker.GetMock<ITrackedDownloadService>()
                  .Setup(s => s.Find("sab1"))
                  .Returns(_trackedDownload);
        }

        [Test]
        public void should_skip_import_if_dronefactory_doesnt_exist()
        {
            Assert.Throws<ArgumentException>(() => Subject.Execute(new DownloadedIssuesScanCommand()));

            Mocker.GetMock<IDownloadedIssuesImportService>().Verify(c => c.ProcessRootFolder(It.IsAny<IDirectoryInfo>()), Times.Never());
        }

        [Test]
        public void should_process_folder_if_downloadclientid_is_not_specified()
        {
            GivenExistingFolder(_downloadFolder);

            Subject.Execute(new DownloadedIssuesScanCommand() { Path = _downloadFolder });

            Mocker.GetMock<IDownloadedIssuesImportService>().Verify(c => c.ProcessPath(It.IsAny<string>(), ImportMode.Auto, null, null), Times.Once());
        }

        [Test]
        public void should_process_file_if_downloadclientid_is_not_specified()
        {
            GivenExistingFile(_downloadFile);

            Subject.Execute(new DownloadedIssuesScanCommand() { Path = _downloadFile });

            Mocker.GetMock<IDownloadedIssuesImportService>().Verify(c => c.ProcessPath(It.IsAny<string>(), ImportMode.Auto, null, null), Times.Once());
        }

        [Test]
        public void should_process_folder_with_downloadclientitem_if_available()
        {
            GivenExistingFolder(_downloadFolder);
            GivenValidQueueItem();

            Subject.Execute(new DownloadedIssuesScanCommand() { Path = _downloadFolder, DownloadClientId = "sab1" });

            Mocker.GetMock<IDownloadedIssuesImportService>().Verify(c => c.ProcessPath(_downloadFolder, ImportMode.Auto, _trackedDownload.RemoteIssue.Series, _trackedDownload.DownloadItem), Times.Once());
        }

        [Test]
        public void should_process_folder_without_downloadclientitem_if_not_available()
        {
            GivenExistingFolder(_downloadFolder);

            Subject.Execute(new DownloadedIssuesScanCommand() { Path = _downloadFolder, DownloadClientId = "sab1" });

            Mocker.GetMock<IDownloadedIssuesImportService>().Verify(c => c.ProcessPath(_downloadFolder, ImportMode.Auto, null, null), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_warn_if_neither_folder_or_file_exists()
        {
            Subject.Execute(new DownloadedIssuesScanCommand() { Path = _downloadFolder });

            Mocker.GetMock<IDownloadedIssuesImportService>().Verify(c => c.ProcessPath(It.IsAny<string>(), ImportMode.Auto, null, null), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_override_import_mode()
        {
            GivenExistingFile(_downloadFile);

            Subject.Execute(new DownloadedIssuesScanCommand() { Path = _downloadFile, ImportMode = ImportMode.Copy });

            Mocker.GetMock<IDownloadedIssuesImportService>().Verify(c => c.ProcessPath(It.IsAny<string>(), ImportMode.Copy, null, null), Times.Once());
        }
    }
}
