using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class MediaFileRepositoryFixture : DbTest<MediaFileRepository, ComicFile>
    {
        private Series _series;
        private Issue _issue;

        [SetUp]
        public void Setup()
        {
            var meta = Builder<SeriesMetadata>.CreateNew()
                .With(a => a.Id = 0)
                .Build();
            Db.Insert(meta);

            _series = Builder<Series>.CreateNew()
                .With(a => a.SeriesMetadataId = meta.Id)
                .With(a => a.Id = 0)
                .Build();
            Db.Insert(_series);

            _issue = Builder<Issue>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.SeriesMetadataId = _series.SeriesMetadataId)
                .Build();
            Db.Insert(_issue);

            var files = Builder<ComicFile>.CreateListOfSize(10)
                .All()
                .With(c => c.Id = 0)
                .With(c => c.Quality = new QualityModel(Quality.CBR))
                .TheFirst(5)
                .With(c => c.IssueId = _issue.Id)
                .TheRest()
                .With(c => c.IssueId = 0)
                .TheFirst(1)
                .With(c => c.Path = @"C:\Test\Path\Series\somefile1.flac".AsOsAgnostic())
                .TheNext(1)
                .With(c => c.Path = @"C:\Test\Path\Series\somefile2.flac".AsOsAgnostic())
                .BuildListOfNew();
            Db.InsertMany(files);
        }

        [Test]
        public void get_files_by_author()
        {
            VerifyData();
            var seriesFiles = Subject.GetFilesBySeries(_series.Id);

            seriesFiles.Should().OnlyContain(c => c.Series.Value.Id == _series.Id);
        }

        [Test]
        public void get_unmapped_files()
        {
            VerifyData();
            var unmappedfiles = Subject.GetUnmappedFiles();

            unmappedfiles.Should().HaveCount(5);
        }

        [TestCase("C:\\Test\\Path")]
        [TestCase("C:\\Test\\Path\\")]
        public void get_files_by_base_path_should_cope_with_trailing_slash(string dir)
        {
            VerifyData();
            var firstReleaseFiles = Subject.GetFilesWithBasePath(dir.AsOsAgnostic());

            firstReleaseFiles.Should().HaveCount(2);
        }

        [TestCase("C:\\Test\\Path")]
        [TestCase("C:\\Test\\Path\\")]
        public void get_files_by_base_path_should_not_get_files_for_partial_path(string dir)
        {
            VerifyData();

            var files = Builder<ComicFile>.CreateListOfSize(2)
                .All()
                .With(c => c.Id = 0)
                .With(c => c.Quality = new QualityModel(Quality.CBR))
                .TheFirst(1)
                .With(c => c.Path = @"C:\Test\Path2\Series\somefile1.flac".AsOsAgnostic())
                .TheNext(1)
                .With(c => c.Path = @"C:\Test\Path2\Series\somefile2.flac".AsOsAgnostic())
                .BuildListOfNew();
            Db.InsertMany(files);

            var firstReleaseFiles = Subject.GetFilesWithBasePath(dir.AsOsAgnostic());
            firstReleaseFiles.Should().HaveCount(2);
        }

        [Test]
        public void get_file_by_path()
        {
            VerifyData();
            var file = Subject.GetFileWithPath(@"C:\Test\Path\Series\somefile2.flac".AsOsAgnostic());

            file.Should().NotBeNull();
            file.Issue.IsLoaded.Should().BeTrue();
            file.Issue.Value.Should().NotBeNull();
            file.Series.IsLoaded.Should().BeTrue();
            file.Series.Value.Should().NotBeNull();
        }

        [Test]
        public void get_files_by_book()
        {
            VerifyData();
            var files = Subject.GetFilesByIssue(_issue.Id);

            files.Should().OnlyContain(c => c.IssueId == _issue.Id);
        }

        private void VerifyData()
        {
            Db.All<Series>().Should().HaveCount(1);
            Db.All<Issue>().Should().HaveCount(1);
            Db.All<ComicFile>().Should().HaveCount(10);
        }

        [Ignore("Doesn't make sense now we link directly to issue")]
        [Test]
        public void delete_files_by_book_should_work_if_join_fails()
        {
            Db.Delete(_issue);
            Subject.DeleteFilesByIssue(_issue.Id);

            Db.All<ComicFile>().Where(x => x.IssueId == _issue.Id).Should().HaveCount(0);
        }
    }
}
