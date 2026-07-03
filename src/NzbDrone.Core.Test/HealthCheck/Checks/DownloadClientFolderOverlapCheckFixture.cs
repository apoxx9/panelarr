using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Download;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class DownloadClientFolderOverlapCheckFixture : CoreTest<DownloadClientFolderOverlapCheck>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>()))
                  .Returns("Some Warning Message");
        }

        private Mock<IDownloadClient> GivenClient(string name, string folder)
        {
            var client = new Mock<IDownloadClient>();

            client.Setup(s => s.Definition)
                  .Returns(new DownloadClientDefinition { Name = name });

            client.Setup(s => s.GetStatus())
                  .Returns(new DownloadClientInfo
                  {
                      OutputRootFolders = new List<OsPath> { new OsPath(folder.AsOsAgnostic()) }
                  });

            return client;
        }

        private void GivenClients(params Mock<IDownloadClient>[] clients)
        {
            var objects = new List<IDownloadClient>();

            foreach (var client in clients)
            {
                objects.Add(client.Object);
            }

            Mocker.GetMock<IProvideDownloadClient>()
                  .Setup(s => s.GetDownloadClients(It.IsAny<bool>()))
                  .Returns(objects);
        }

        [Test]
        public void should_warn_when_one_client_downloads_inside_anothers_folder()
        {
            GivenClients(
                GivenClient("Seedbox", @"c:\downloads"),
                GivenClient("GetComics", @"c:\downloads\getcomics"));

            Subject.Check().ShouldBeWarning();
        }

        [Test]
        public void should_warn_when_two_clients_share_a_folder()
        {
            GivenClients(
                GivenClient("Seedbox", @"c:\downloads"),
                GivenClient("GetComics", @"c:\downloads"));

            Subject.Check().ShouldBeWarning();
        }

        [Test]
        public void should_be_ok_for_separate_folders()
        {
            GivenClients(
                GivenClient("Seedbox", @"c:\downloads"),
                GivenClient("GetComics", @"c:\data\getcomics"));

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_be_ok_with_a_single_client()
        {
            GivenClients(GivenClient("GetComics", @"c:\downloads\getcomics"));

            Subject.Check().ShouldBeOk();
        }
    }
}
