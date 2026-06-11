using System;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ProviderTests.RecycleBinProviderTests
{
    [TestFixture]

    public class DeleteFileFixture : CoreTest
    {
        private void WithRecycleBin()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBin).Returns(@"C:\Test\Recycle Bin".AsOsAgnostic());
        }

        private void WithoutRecycleBin()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.RecycleBin).Returns(string.Empty);
        }

        [Test]
        public void should_use_delete_when_recycleBin_is_not_configured()
        {
            WithoutRecycleBin();

            var path = @"C:\Test\Comics\Saga (2012)\Saga 001 (2012) (Digital) (Empire).cbz".AsOsAgnostic();

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.DeleteFile(path), Times.Once());
        }

        [Test]
        public void should_use_move_when_recycleBin_is_configured()
        {
            WithRecycleBin();

            var path = @"C:\Test\Comics\Saga (2012)\Saga 001 (2012) (Digital) (Empire).cbz".AsOsAgnostic();

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, @"C:\Test\Recycle Bin\Saga 001 (2012) (Digital) (Empire).cbz".AsOsAgnostic(), TransferMode.Move, false), Times.Once());
        }

        [Test]
        public void should_use_alternative_name_if_already_exists()
        {
            WithRecycleBin();

            var path = @"C:\Test\Comics\Saga (2012)\Saga 001 (2012) (Digital) (Empire).cbz".AsOsAgnostic();

            Mocker.GetMock<IDiskProvider>()
                .Setup(v => v.FileExists(@"C:\Test\Recycle Bin\Saga 001 (2012) (Digital) (Empire).cbz".AsOsAgnostic()))
                .Returns(true);

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, @"C:\Test\Recycle Bin\Saga 001 (2012) (Digital) (Empire)_2.cbz".AsOsAgnostic(), TransferMode.Move, false), Times.Once());
        }

        [Test]
        public void should_call_fileSetLastWriteTime_for_each_file()
        {
            WindowsOnly();
            WithRecycleBin();
            var path = @"C:\Test\Comics\Saga (2012)\Saga 001 (2012) (Digital) (Empire).cbz".AsOsAgnostic();

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path);

            Mocker.GetMock<IDiskProvider>().Verify(v => v.FileSetLastWriteTime(@"C:\Test\Recycle Bin\Saga 001 (2012) (Digital) (Empire).cbz".AsOsAgnostic(), It.IsAny<DateTime>()), Times.Once());
        }

        [Test]
        public void should_use_subfolder_when_passed_in()
        {
            WithRecycleBin();

            var path = @"C:\Test\Comics\Saga (2012)\Saga 001 (2012) (Digital) (Empire).cbz".AsOsAgnostic();

            Mocker.Resolve<RecycleBinProvider>().DeleteFile(path, "Saga (2012)");

            Mocker.GetMock<IDiskTransferService>().Verify(v => v.TransferFile(path, @"C:\Test\Recycle Bin\Saga (2012)\Saga 001 (2012) (Digital) (Empire).cbz".AsOsAgnostic(), TransferMode.Move, false), Times.Once());
        }
    }
}
