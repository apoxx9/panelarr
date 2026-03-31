using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.SeriesStats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.SeriesStatsTests
{
    [TestFixture]
    public class SeriesStatisticsFixture : DbTest<SeriesStatisticsRepository, Series>
    {
        private Series _series;
        private Issue _issue;
        private List<ComicFile> _comicFiles;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>.CreateNew()
                .With(a => a.SeriesMetadataId = 10)
                .BuildNew();
            Db.Insert(_series);

            _issue = Builder<Issue>.CreateNew()
                .With(e => e.ReleaseDate = DateTime.Today.AddDays(-5))
                .With(e => e.SeriesMetadataId = 10)
                .BuildNew();
            Db.Insert(_issue);

            _comicFiles = Builder<ComicFile>.CreateListOfSize(2)
                .All()
                .With(x => x.Id = 0)
                .With(e => e.Series = _series)
                .With(e => e.IssueId = _issue.Id)
                .With(e => e.Quality = new QualityModel(Quality.CBR))
                .BuildList();
        }

        private void GivenComicFile()
        {
            Db.Insert(_comicFiles[0]);
        }

        private void GivenTwoComicFiles()
        {
            Db.InsertMany(_comicFiles);
        }

        [Test]
        public void should_get_stats_for_series()
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
            GivenComicFile();

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
            GivenComicFile();

            var stats = Subject.SeriesStatistics();

            stats.Should().HaveCount(1);
            stats.First().SizeOnDisk.Should().Be(_comicFiles[0].Size);
        }

        [Test]
        public void should_count_book_with_two_files_as_one_book()
        {
            GivenTwoComicFiles();

            var stats = Subject.SeriesStatistics();

            Db.All<ComicFile>().Should().HaveCount(2);
            stats.Should().HaveCount(1);

            var bookStats = stats.First();

            bookStats.TotalIssueCount.Should().Be(1);
            bookStats.IssueCount.Should().Be(1);
            bookStats.AvailableIssueCount.Should().Be(1);
            bookStats.SizeOnDisk.Should().Be(_comicFiles.Sum(x => x.Size));
            bookStats.ComicFileCount.Should().Be(2);
        }
    }
}
