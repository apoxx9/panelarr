using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.SeriesStats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.SeriesStatsTests
{
    [TestFixture]
    public class SeriesStatisticsFixture : DbTest<SeriesStatisticsRepository, Series>
    {
        private Series _author;
        private Issue _book;
        private List<ComicFile> _bookFiles;

        [SetUp]
        public void Setup()
        {
            _author = Builder<Series>.CreateNew()
                .With(a => a.SeriesMetadataId = 10)
                .BuildNew();
            Db.Insert(_author);

            _book = Builder<Issue>.CreateNew()
                .With(e => e.ReleaseDate = DateTime.Today.AddDays(-5))
                .With(e => e.SeriesMetadataId = 10)
                .BuildNew();
            Db.Insert(_book);

            _bookFiles = Builder<ComicFile>.CreateListOfSize(2)
                .All()
                .With(x => x.Id = 0)
                .With(e => e.Series = _author)
                .With(e => e.IssueId = _book.Id)
                .With(e => e.Quality = new QualityModel(Quality.CBR))
                .BuildList();
        }

        private void GivenBookFile()
        {
            Db.Insert(_bookFiles[0]);
        }

        private void GivenTwoBookFiles()
        {
            Db.InsertMany(_bookFiles);
        }

        [Test]
        public void should_get_stats_for_author()
        {
            var stats = Subject.SeriesStatistics();

            stats.Should().HaveCount(1);
        }

        [Test]
        public void should_not_include_unmonitored_book_in_book_count()
        {
            var stats = Subject.SeriesStatistics();

            stats.Should().HaveCount(1);
            stats.First().IssueCount.Should().Be(0);
        }

        [Test]
        public void should_include_unmonitored_book_with_file_in_book_count()
        {
            GivenBookFile();

            var stats = Subject.SeriesStatistics();

            stats.Should().HaveCount(1);
            stats.First().IssueCount.Should().Be(1);
        }

        [Test]
        public void should_have_size_on_disk_of_zero_when_no_book_file()
        {
            var stats = Subject.SeriesStatistics();

            stats.Should().HaveCount(1);
            stats.First().SizeOnDisk.Should().Be(0);
        }

        [Test]
        public void should_have_size_on_disk_when_book_file_exists()
        {
            GivenBookFile();

            var stats = Subject.SeriesStatistics();

            stats.Should().HaveCount(1);
            stats.First().SizeOnDisk.Should().Be(_bookFiles[0].Size);
        }

        [Test]
        public void should_count_book_with_two_files_as_one_book()
        {
            GivenTwoBookFiles();

            var stats = Subject.SeriesStatistics();

            Db.All<ComicFile>().Should().HaveCount(2);
            stats.Should().HaveCount(1);

            var bookStats = stats.First();

            bookStats.TotalBookCount.Should().Be(1);
            bookStats.IssueCount.Should().Be(1);
            bookStats.AvailableBookCount.Should().Be(1);
            bookStats.SizeOnDisk.Should().Be(_bookFiles.Sum(x => x.Size));
            bookStats.ComicFileCount.Should().Be(2);
        }
    }
}
