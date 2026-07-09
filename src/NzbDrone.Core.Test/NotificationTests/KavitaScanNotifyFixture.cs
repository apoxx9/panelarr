using System.Collections.Generic;
using System.IO;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Notifications.Kavita;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class KavitaScanNotifyFixture : CoreTest<Kavita>
    {
        private string _folder;

        [SetUp]
        public void Setup()
        {
            _folder = @"C:\comics\Publisher\Series (2026)".AsOsAgnostic();

            Subject.Definition = new NotificationDefinition
            {
                Settings = new KavitaSettings
                {
                    Host = "localhost",
                    Port = 5000,
                    ApiKey = "key",
                    Notify = true
                }
            };
        }

        private IssueDownloadMessage GivenImportMessage()
        {
            return new IssueDownloadMessage
            {
                ComicFiles = new List<ComicFile>
                {
                    new ComicFile { Path = Path.Combine(_folder, "Series 001 (2026).cbz") }
                }
            };
        }

        [Test]
        public void on_release_import_should_send_bare_folder_path()
        {
            // Kavita's scan-folder endpoint 500s on anything that isn't a real
            // library path — no notification-title prefixes
            Subject.OnReleaseImport(GivenImportMessage());

            Mocker.GetMock<IKavitaService>()
                  .Verify(s => s.Notify(It.IsAny<KavitaSettings>(), _folder), Times.Once());
        }

        [Test]
        public void should_not_notify_when_disabled()
        {
            ((KavitaSettings)Subject.Definition.Settings).Notify = false;

            Subject.OnReleaseImport(GivenImportMessage());

            Mocker.GetMock<IKavitaService>()
                  .Verify(s => s.Notify(It.IsAny<KavitaSettings>(), It.IsAny<string>()), Times.Never());
        }
    }
}
