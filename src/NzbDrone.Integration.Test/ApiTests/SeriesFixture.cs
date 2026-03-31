using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    [Ignore("Waiting for metadata to be back again", Until = "2026-01-15 00:00:00Z")]
    public class SeriesFixture : IntegrationTest
    {
        [Test]
        [Order(0)]
        public void add_author_with_tags_should_store_them()
        {
            EnsureNoSeries("14586394", "Andrew Hunter Murray");
            var tag = EnsureTag("abc");

            var series = Series.Lookup("edition:43765115").Single();

            series.QualityProfileId = 1;
            series.Path = Path.Combine(SeriesRootFolder, series.SeriesName);
            series.Tags = new HashSet<int>();
            series.Tags.Add(tag.Id);

            var result = Series.Post(series);

            result.Should().NotBeNull();
            result.Tags.Should().Equal(tag.Id);
        }

        [Test]
        [Order(0)]
        public void add_author_without_profileid_should_return_badrequest()
        {
            EnsureNoSeries("14586394", "Andrew Hunter Murray");

            var series = Series.Lookup("edition:43765115").Single();

            series.Path = Path.Combine(SeriesRootFolder, series.SeriesName);

            Series.InvalidPost(series);
        }

        [Test]
        [Order(0)]
        public void add_author_without_path_should_return_badrequest()
        {
            EnsureNoSeries("14586394", "Andrew Hunter Murray");

            var series = Series.Lookup("edition:43765115").Single();

            series.QualityProfileId = 1;

            Series.InvalidPost(series);
        }

        [Test]
        [Order(1)]
        public void add_author()
        {
            EnsureNoSeries("14586394", "Andrew Hunter Murray");

            var series = Series.Lookup("edition:43765115").Single();

            series.QualityProfileId = 1;
            series.Path = Path.Combine(SeriesRootFolder, series.SeriesName);

            var result = Series.Post(series);

            result.Should().NotBeNull();
            result.Id.Should().NotBe(0);
            result.QualityProfileId.Should().Be(1);
            result.Path.Should().Be(Path.Combine(SeriesRootFolder, series.SeriesName));
        }

        [Test]
        [Order(2)]
        public void get_all_author()
        {
            EnsureSeries("14586394", "43765115", "Andrew Hunter Murray");
            EnsureSeries("383606", "16160797", "Robert Galbraith");

            var allSeries = Series.All();

            allSeries.Should().NotBeNullOrEmpty();
            allSeries.Should().Contain(v => v.ForeignSeriesId == "14586394");
            allSeries.Should().Contain(v => v.ForeignSeriesId == "383606");
        }

        [Test]
        [Order(2)]
        public void get_author_by_id()
        {
            var series = EnsureSeries("14586394", "43765115", "Andrew Hunter Murray");

            var result = Series.Get(series.Id);

            result.ForeignSeriesId.Should().Be("14586394");
        }

        [Test]
        public void get_author_by_unknown_id_should_return_404()
        {
            var result = Series.InvalidGet(1000000);
        }

        [Test]
        [Order(2)]
        public void update_series_profile_id()
        {
            var series = EnsureSeries("14586394", "43765115", "Andrew Hunter Murray");

            var profileId = 1;
            if (series.QualityProfileId == profileId)
            {
                profileId = 2;
            }

            series.QualityProfileId = profileId;

            var result = Series.Put(series);

            Series.Get(series.Id).QualityProfileId.Should().Be(profileId);
        }

        [Test]
        [Order(3)]
        public void update_series_monitored()
        {
            var series = EnsureSeries("14586394", "43765115", "Andrew Hunter Murray", false);

            series.Monitored.Should().BeFalse();

            series.Monitored = true;

            var result = Series.Put(series);

            result.Monitored.Should().BeTrue();
        }

        [Test]
        [Order(3)]
        public void update_series_tags()
        {
            var series = EnsureSeries("14586394", "43765115", "Andrew Hunter Murray");
            var tag = EnsureTag("abc");

            if (series.Tags.Contains(tag.Id))
            {
                series.Tags.Remove(tag.Id);

                var result = Series.Put(series);
                Series.Get(series.Id).Tags.Should().NotContain(tag.Id);
            }
            else
            {
                series.Tags.Add(tag.Id);

                var result = Series.Put(series);
                Series.Get(series.Id).Tags.Should().Contain(tag.Id);
            }
        }

        [Test]
        [Order(4)]
        public void delete_author()
        {
            var series = EnsureSeries("14586394", "43765115", "Andrew Hunter Murray");

            Series.Get(series.Id).Should().NotBeNull();

            Series.Delete(series.Id);

            Series.All().Should().NotContain(v => v.ForeignSeriesId == "14586394");
        }
    }
}
