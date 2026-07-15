using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.ComicInfo;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class ComicFileConversionServiceFixture : FileSystemTest<ComicFileConversionService>
    {
        private Series _series;
        private ComicFile _cbrFile;
        private string _folder;

        [SetUp]
        public void Setup()
        {
            _folder = @"C:\comics\Marvel\Iron Man Epic Collection".AsOsAgnostic();

            _series = new Series { Id = 5, Path = _folder };

            _cbrFile = new ComicFile
            {
                Id = 11,
                IssueId = 100,
                Path = (_folder + @"\Iron Man Epic Collection v01 - The Golden Avenger.cbr").AsOsAgnostic(),
                ComicFormat = ComicFormat.CBR
            };

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(5))
                  .Returns(_series);

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesWithBasePath(_folder))
                  .Returns(new List<ComicFile> { _cbrFile });
        }

        private void GivenConversion(ComicFile file, string newPath)
        {
            FileSystem.AddFile(newPath, new MockFileData("".PadRight(42)));

            Mocker.GetMock<IComicFormatConverter>()
                  .Setup(s => s.ConvertToRealCbz(file.Path))
                  .Returns(new ComicFormatConversionResult { FinalPath = newPath, Changed = true });
        }

        [Test]
        public void should_update_row_and_embed_after_conversion()
        {
            var newPath = _cbrFile.Path.Replace(".cbr", ".cbz");
            GivenConversion(_cbrFile, newPath);

            Subject.Execute(new ConvertComicFilesCommand { SeriesIds = new List<int> { 5 } });

            _cbrFile.Path.Should().Be(newPath);
            _cbrFile.ComicFormat.Should().Be(ComicFormat.CBZ);
            _cbrFile.Size.Should().Be(42);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Update(_cbrFile), Times.Once());

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(_cbrFile), Times.Once());
        }

        [Test]
        public void should_not_embed_for_unmapped_file()
        {
            _cbrFile.IssueId = 0;

            var newPath = _cbrFile.Path.Replace(".cbr", ".cbz");
            GivenConversion(_cbrFile, newPath);

            Subject.Execute(new ConvertComicFilesCommand { SeriesIds = new List<int> { 5 } });

            _cbrFile.ComicFormat.Should().Be(ComicFormat.CBZ);

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(It.IsAny<ComicFile>()), Times.Never());
        }

        [Test]
        public void should_skip_and_count_failures_without_touching_row()
        {
            Mocker.GetMock<IComicFormatConverter>()
                  .Setup(s => s.ConvertToRealCbz(_cbrFile.Path))
                  .Returns(new ComicFormatConversionResult { FinalPath = _cbrFile.Path, Error = "target already exists" });

            Subject.Execute(new ConvertComicFilesCommand { SeriesIds = new List<int> { 5 } });

            _cbrFile.ComicFormat.Should().Be(ComicFormat.CBR);

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Update(It.IsAny<ComicFile>()), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_convert_shared_folder_file_once_across_series()
        {
            var other = new Series { Id = 6, Path = _folder };

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(6))
                  .Returns(other);

            var newPath = _cbrFile.Path.Replace(".cbr", ".cbz");
            GivenConversion(_cbrFile, newPath);

            Subject.Execute(new ConvertComicFilesCommand { SeriesIds = new List<int> { 5, 6 } });

            Mocker.GetMock<IComicFormatConverter>()
                  .Verify(s => s.ConvertToRealCbz(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_retag_already_cbz_mapped_files_without_conversion()
        {
            _cbrFile.ComicFormat = ComicFormat.CBZ;
            _cbrFile.Path = _cbrFile.Path.Replace(".cbr", ".cbz");

            Mocker.GetMock<IComicFormatConverter>()
                  .Setup(s => s.ConvertToRealCbz(_cbrFile.Path))
                  .Returns(new ComicFormatConversionResult { FinalPath = _cbrFile.Path, Changed = false });

            Subject.Execute(new ConvertComicFilesCommand { SeriesIds = new List<int> { 5 } });

            Mocker.GetMock<IMediaFileService>()
                  .Verify(s => s.Update(It.IsAny<ComicFile>()), Times.Never());

            Mocker.GetMock<IComicInfoEmbedService>()
                  .Verify(s => s.EmbedMetadata(_cbrFile), Times.Once());
        }
    }
}
