using System.Collections.Generic;
using System.IO;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Notifications.Synology;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class SynologyIndexerFixture : CoreTest<SynologyIndexer>
    {
        private Series _series;
        private IssueDownloadMessage _upgrade;
        private string _rootPath = @"C:\Test\".AsOsAgnostic();

        [SetUp]
        public void SetUp()
        {
            _series = new Series()
            {
                Path = _rootPath,
            };

            _upgrade = new IssueDownloadMessage()
            {
                Series = _series,

                ComicFiles = new List<ComicFile>
                {
                    new ComicFile
                    {
                        Path = Path.Combine(_rootPath, "file1.S01E01E02.mkv")
                    }
                },

                OldFiles = new List<ComicFile>
                {
                    new ComicFile
                    {
                        Path = Path.Combine(_rootPath, "file1.S01E01.mkv")
                    },
                    new ComicFile
                    {
                        Path = Path.Combine(_rootPath, "file1.S01E02.mkv")
                    }
                }
            };

            Subject.Definition = new NotificationDefinition
            {
                Settings = new SynologyIndexerSettings
                {
                    UpdateLibrary = true
                }
            };
        }

        [Test]
        public void should_not_update_library_if_disabled()
        {
            (Subject.Definition.Settings as SynologyIndexerSettings).UpdateLibrary = false;

            Subject.OnRename(_series, new List<RenamedComicFile>());

            Mocker.GetMock<ISynologyIndexerProxy>()
                .Verify(v => v.UpdateFolder(_series.Path), Times.Never());
        }

        [Test]
        public void should_remove_old_episodes_on_upgrade()
        {
            Subject.OnReleaseImport(_upgrade);

            Mocker.GetMock<ISynologyIndexerProxy>()
                .Verify(v => v.DeleteFile(@"C:\Test\file1.S01E01.mkv".AsOsAgnostic()), Times.Once());

            Mocker.GetMock<ISynologyIndexerProxy>()
                .Verify(v => v.DeleteFile(@"C:\Test\file1.S01E02.mkv".AsOsAgnostic()), Times.Once());
        }

        [Test]
        public void should_add_new_episode_on_upgrade()
        {
            Subject.OnReleaseImport(_upgrade);

            Mocker.GetMock<ISynologyIndexerProxy>()
                .Verify(v => v.AddFile(@"C:\Test\file1.S01E01E02.mkv".AsOsAgnostic()), Times.Once());
        }

        [Test]
        public void should_update_entire_series_folder_on_rename()
        {
            Subject.OnRename(_series, new List<RenamedComicFile>());

            Mocker.GetMock<ISynologyIndexerProxy>()
                .Verify(v => v.UpdateFolder(@"C:\Test\".AsOsAgnostic()), Times.Once());
        }
    }
}
