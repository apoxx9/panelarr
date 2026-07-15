using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    public class UpgradeMediaFileServiceFixture : CoreTest<UpgradeMediaFileService>
    {
        private ComicFile _trackFile;
        private LocalIssue _localTrack;
        private string _rootPath = @"C:\Test\Comics\Saga (2012)".AsOsAgnostic();

        [SetUp]
        public void Setup()
        {
            _localTrack = new LocalIssue();
            _localTrack.Series = new Series
            {
                Path = _rootPath
            };

            _trackFile = Builder<ComicFile>
                .CreateNew()
                .Build();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FolderExists(It.IsAny<string>()))
                .Returns(true);

            Mocker.GetMock<IDiskProvider>()
                 .Setup(c => c.GetParentFolder(It.IsAny<string>()))
                 .Returns<string>(c => Path.GetDirectoryName(c));

            Mocker.GetMock<IRootFolderService>()
                .Setup(c => c.GetBestRootFolder(It.IsAny<string>()))
                .Returns(new RootFolder());

            Mocker.GetMock<IRootFolderService>()
                .Setup(c => c.GetBestRootFolderPath(It.IsAny<string>()))
                .Returns<string>(c => Path.GetDirectoryName(c));
        }

        private void GivenSingleIssueWithSingleComicFile()
        {
            _localTrack.Issue = Builder<Issue>.CreateNew()
                .With(e => e.ComicFiles = new LazyLoaded<List<ComicFile>>(
                          new List<ComicFile>
                          {
                              new ComicFile
                              {
                                  Id = 1,
                                  Path = Path.Combine(_rootPath, @"Volume 01\Saga 001 (2012) (Digital) (Empire).cbz"),
                              }
                          }))
                .Build();
        }

        [Test]
        public void should_delete_single_track_file_once()
        {
            GivenSingleIssueWithSingleComicFile();

            Subject.UpgradeComicFile(_trackFile, _localTrack);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_delete_track_file_from_database()
        {
            GivenSingleIssueWithSingleComicFile();

            Subject.UpgradeComicFile(_trackFile, _localTrack);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(It.IsAny<ComicFile>(), DeleteMediaFileReason.Upgrade), Times.Once());
        }

        [Test]
        public void should_delete_existing_file_fromdb_if_file_doesnt_exist()
        {
            GivenSingleIssueWithSingleComicFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeComicFile(_trackFile, _localTrack);

            // Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localTrack.Issue.ComicFiles.Value, DeleteMediaFileReason.Upgrade), Times.Once());
        }

        [Test]
        public void should_not_try_to_recyclebin_existing_file_if_file_doesnt_exist()
        {
            GivenSingleIssueWithSingleComicFile();

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.FileExists(It.IsAny<string>()))
                .Returns(false);

            Subject.UpgradeComicFile(_trackFile, _localTrack);

            Mocker.GetMock<IRecycleBinProvider>().Verify(v => v.DeleteFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_return_old_track_file_in_oldFiles()
        {
            GivenSingleIssueWithSingleComicFile();

            Subject.UpgradeComicFile(_trackFile, _localTrack).OldFiles.Count.Should().Be(1);
        }

        [Test]
        [Ignore("Pending panelarr fix")]
        public void should_import_if_existing_file_doesnt_exist_in_db()
        {
            _localTrack.Issue = Builder<Issue>.CreateNew()
                .With(e => e.ComicFiles = new LazyLoaded<List<ComicFile>>())
                .Build();

            Subject.UpgradeComicFile(_trackFile, _localTrack);

            // Mocker.GetMock<IMediaFileService>().Verify(v => v.Delete(_localTrack.Issue.ComicFiles.Value, It.IsAny<DeleteMediaFileReason>()), Times.Never());
        }

        [Test]
        public void should_not_convert_when_convert_to_cbz_is_disabled()
        {
            GivenSingleIssueWithSingleComicFile();

            Subject.UpgradeComicFile(_trackFile, _localTrack);

            Mocker.GetMock<IComicFormatConverter>().Verify(v => v.ConvertToRealCbz(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_convert_before_writing_tags_when_convert_to_cbz_is_enabled()
        {
            GivenSingleIssueWithSingleComicFile();
            GivenConvertToCbz();

            var importedPath = Path.Combine(_rootPath, "Saga 002 (2012).cbr").AsOsAgnostic();
            var convertedPath = Path.Combine(_rootPath, "Saga 002 (2012).cbz").AsOsAgnostic();
            _trackFile.Path = importedPath;
            _trackFile.ComicFormat = ComicFormat.CBR;

            Mocker.GetMock<IComicFormatConverter>()
                .Setup(c => c.ConvertToRealCbz(importedPath))
                .Returns(new ComicFormatConversionResult { FinalPath = convertedPath, Changed = true });

            Mocker.GetMock<IDiskProvider>()
                .Setup(c => c.GetFileSize(convertedPath))
                .Returns(1234);

            string pathAtTagTime = null;
            Mocker.GetMock<IMetadataTagService>()
                .Setup(c => c.WriteTags(It.IsAny<ComicFile>(), true, false))
                .Callback<ComicFile, bool, bool>((f, n, force) => pathAtTagTime = f.Path);

            Subject.UpgradeComicFile(_trackFile, _localTrack);

            _trackFile.Path.Should().Be(convertedPath);
            _trackFile.ComicFormat.Should().Be(ComicFormat.CBZ);
            _trackFile.Size.Should().Be(1234);
            pathAtTagTime.Should().Be(convertedPath);
        }

        [Test]
        public void should_not_fail_import_when_conversion_fails()
        {
            GivenSingleIssueWithSingleComicFile();
            GivenConvertToCbz();

            var importedPath = Path.Combine(_rootPath, "Saga 002 (2012).cbr").AsOsAgnostic();
            _trackFile.Path = importedPath;
            _trackFile.ComicFormat = ComicFormat.CBR;

            Mocker.GetMock<IComicFormatConverter>()
                .Setup(c => c.ConvertToRealCbz(importedPath))
                .Returns(new ComicFormatConversionResult { FinalPath = importedPath, Error = "verification failed" });

            Subject.UpgradeComicFile(_trackFile, _localTrack);

            _trackFile.Path.Should().Be(importedPath);
            _trackFile.ComicFormat.Should().Be(ComicFormat.CBR);
            Mocker.GetMock<IMetadataTagService>().Verify(v => v.WriteTags(_trackFile, true, false), Times.Once());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_touch_file_details_when_conversion_changed_nothing()
        {
            GivenSingleIssueWithSingleComicFile();
            GivenConvertToCbz();

            var importedPath = Path.Combine(_rootPath, "Saga 002 (2012).cbz").AsOsAgnostic();
            _trackFile.Path = importedPath;
            _trackFile.ComicFormat = ComicFormat.CBZ;

            Mocker.GetMock<IComicFormatConverter>()
                .Setup(c => c.ConvertToRealCbz(importedPath))
                .Returns(new ComicFormatConversionResult { FinalPath = importedPath, Changed = false });

            Subject.UpgradeComicFile(_trackFile, _localTrack);

            _trackFile.Path.Should().Be(importedPath);
            Mocker.GetMock<IDiskProvider>().Verify(v => v.GetFileSize(It.IsAny<string>()), Times.Never());
        }

        private void GivenConvertToCbz()
        {
            Mocker.GetMock<NzbDrone.Core.Configuration.IConfigService>()
                .Setup(c => c.ConvertToCbz)
                .Returns(true);
        }
    }
}
