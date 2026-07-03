using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.MediaFileDeletionServiceTests
{
    [TestFixture]
    public class DeleteSeriesFixture : CoreTest<NzbDrone.Core.MediaFiles.MediaFileDeletionService>
    {
        private Series _series;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>.CreateNew()
                                     .With(s => s.Path = @"C:\comics\The Walking Dead (2004)".AsOsAgnostic())
                                     .Build();

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.AllSeriesPaths())
                  .Returns(new Dictionary<int, string> { { _series.Id, _series.Path } });
        }

        [Test]
        public void keeping_files_should_unlink_them_so_they_return_to_library_import()
        {
            var files = Builder<ComicFile>.CreateListOfSize(3)
                                          .All()
                                          .With(f => f.IssueId = 42)
                                          .Build()
                                          .ToList();

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesWithBasePath(_series.Path))
                  .Returns(files);

            Subject.HandleAsync(new SeriesDeletedEvent(_series, false, false));

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Update(It.Is<List<ComicFile>>(l => l.Count == 3 && l.TrueForAll(f => f.IssueId == 0))),
                          Times.Once());
        }

        [Test]
        public void keeping_files_should_not_touch_already_unmapped_rows()
        {
            var files = Builder<ComicFile>.CreateListOfSize(2)
                                          .All()
                                          .With(f => f.IssueId = 0)
                                          .Build()
                                          .ToList();

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesWithBasePath(_series.Path))
                  .Returns(files);

            Subject.HandleAsync(new SeriesDeletedEvent(_series, false, false));

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Update(It.IsAny<List<ComicFile>>()), Times.Never());
        }

        [Test]
        public void deleting_files_should_not_unlink()
        {
            Subject.HandleAsync(new SeriesDeletedEvent(_series, true, false));

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Update(It.IsAny<List<ComicFile>>()), Times.Never());
        }
    }
}
