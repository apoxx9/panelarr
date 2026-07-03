using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients.GetComics;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download.DownloadClientTests.GetComicsTests
{
    [TestFixture]
    public class GetComicsDownloadClientFixture : DownloadClientFixtureBase<GetComicsDownloadClient>
    {
        private string _downloadFolder;
        private string _parentFolder;

        [SetUp]
        public void Setup()
        {
            _parentFolder = @"c:\downloads".AsOsAgnostic();
            _downloadFolder = @"c:\downloads\getcomics".AsOsAgnostic();

            Subject.Definition = new DownloadClientDefinition
            {
                Settings = new GetComicsDownloadClientSettings
                {
                    DownloadFolder = _downloadFolder
                }
            };
        }

        private void GivenFolderExists(string folder, bool exists)
        {
            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(folder))
                  .Returns(exists);
        }

        [Test]
        public void test_should_create_missing_download_folder_when_parent_exists()
        {
            GivenFolderExists(_downloadFolder, false);
            GivenFolderExists(_parentFolder, true);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetParentFolder(_downloadFolder))
                  .Returns(_parentFolder);

            Subject.Test();

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.CreateFolder(_downloadFolder), Times.Once());
        }

        [Test]
        public void test_should_not_create_folder_when_parent_missing()
        {
            GivenFolderExists(_downloadFolder, false);
            GivenFolderExists(_parentFolder, false);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.GetParentFolder(_downloadFolder))
                  .Returns(_parentFolder);

            Subject.Test();

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.CreateFolder(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void test_should_not_create_folder_when_it_already_exists()
        {
            GivenFolderExists(_downloadFolder, true);

            Subject.Test();

            Mocker.GetMock<IDiskProvider>()
                  .Verify(s => s.CreateFolder(It.IsAny<string>()), Times.Never());
        }
    }
}
