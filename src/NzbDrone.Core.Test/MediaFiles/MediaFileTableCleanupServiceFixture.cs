using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    public class MediaFileTableCleanupServiceFixture : CoreTest<MediaFileTableCleanupService>
    {
        private readonly string _DELETED_PATH = @"c:\ANY FILE STARTING WITH THIS PATH IS CONSIDERED DELETED!".AsOsAgnostic();
        private List<Issue> _tracks;
        private Series _series;

        [SetUp]
        public void SetUp()
        {
            _tracks = Builder<Issue>.CreateListOfSize(10)
                  .Build()
                  .ToList();

            _series = Builder<Series>.CreateNew()
                                     .With(s => s.Path = @"C:\Test\Music\Series".AsOsAgnostic())
                                     .Build();
        }

        private void GivenComicFiles(IEnumerable<ComicFile> trackFiles)
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(c => c.GetFilesWithBasePath(It.IsAny<string>()))
                  .Returns(trackFiles.ToList());
        }

        private void GivenFilesAreNotAttachedToTrack()
        {
        }

        private List<string> FilesOnDisk(IEnumerable<ComicFile> trackFiles)
        {
            return trackFiles.Select(e => e.Path).ToList();
        }

        [Test]
        public void should_skip_files_that_exist_on_disk()
        {
            var trackFiles = Builder<ComicFile>.CreateListOfSize(10)
                .All()
                .With(x => x.Path = Path.Combine(@"c:\test".AsOsAgnostic(), Path.GetRandomFileName()))
                .Build();

            GivenComicFiles(trackFiles);

            Subject.Clean(_series.Path, FilesOnDisk(trackFiles));

            Mocker.GetMock<IMediaFileService>()
                .Verify(c => c.DeleteMany(It.Is<List<ComicFile>>(x => x.Count == 0), DeleteMediaFileReason.MissingFromDisk), Times.Once());
        }

        [Test]
        public void should_delete_non_existent_files()
        {
            var trackFiles = Builder<ComicFile>.CreateListOfSize(10)
                .All()
                .With(x => x.Path = Path.Combine(@"c:\test".AsOsAgnostic(), Path.GetRandomFileName()))
                .Random(2)
                .With(c => c.Path = Path.Combine(_DELETED_PATH, Path.GetRandomFileName()))
                .Build();

            GivenComicFiles(trackFiles);

            Subject.Clean(_series.Path, FilesOnDisk(trackFiles.Where(e => !e.Path.StartsWith(_DELETED_PATH))));

            Mocker.GetMock<IMediaFileService>()
                .Verify(c => c.DeleteMany(It.Is<List<ComicFile>>(e => e.Count == 2 && e.All(y => y.Path.StartsWith(_DELETED_PATH))), DeleteMediaFileReason.MissingFromDisk), Times.Once());
        }

        [Test]
        public void should_unlink_track_when_trackFile_does_not_exist()
        {
            var trackFiles = Builder<ComicFile>.CreateListOfSize(10)
                .Random(10)
                .With(c => c.Path = Path.Combine(@"c:\test".AsOsAgnostic(), Path.GetRandomFileName()))
                .Build();

            GivenComicFiles(trackFiles);

            Subject.Clean(_series.Path, new List<string>());
        }

        [Test]
        public void should_not_update_track_when_trackFile_exists()
        {
            var trackFiles = Builder<ComicFile>.CreateListOfSize(10)
                                .Random(10)
                                .With(c => c.Path = Path.Combine(@"c:\test".AsOsAgnostic(), Path.GetRandomFileName()))
                                .Build();

            GivenComicFiles(trackFiles);

            Subject.Clean(_series.Path, FilesOnDisk(trackFiles));
        }
    }
}
