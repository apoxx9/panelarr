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

            var author = Series.Lookup("edition:43765115").Single();

            author.QualityProfileId = 1;
            author.Path = Path.Combine(SeriesRootFolder, author.SeriesName);
            author.Tags = new HashSet<int>();
            author.Tags.Add(tag.Id);

            var result = Series.Post(author);

            result.Should().NotBeNull();
            result.Tags.Should().Equal(tag.Id);
        }

        [Test]
        [Order(0)]
        public void add_author_without_profileid_should_return_badrequest()
        {
            EnsureNoSeries("14586394", "Andrew Hunter Murray");

            var author = Series.Lookup("edition:43765115").Single();

            author.Path = Path.Combine(SeriesRootFolder, author.SeriesName);

            Series.InvalidPost(author);
        }

        [Test]
        [Order(0)]
        public void add_author_without_path_should_return_badrequest()
        {
            EnsureNoSeries("14586394", "Andrew Hunter Murray");

            var author = Series.Lookup("edition:43765115").Single();

            author.QualityProfileId = 1;

            Series.InvalidPost(author);
        }

        [Test]
        [Order(1)]
        public void add_author()
        {
            EnsureNoSeries("14586394", "Andrew Hunter Murray");

            var author = Series.Lookup("edition:43765115").Single();

            author.QualityProfileId = 1;
            author.Path = Path.Combine(SeriesRootFolder, author.SeriesName);

            var result = Series.Post(author);

            result.Should().NotBeNull();
            result.Id.Should().NotBe(0);
            result.QualityProfileId.Should().Be(1);
            result.Path.Should().Be(Path.Combine(SeriesRootFolder, author.SeriesName));
        }

        [Test]
        [Order(2)]
        public void get_all_author()
        {
            EnsureSeries("14586394", "43765115", "Andrew Hunter Murray");
            EnsureSeries("383606", "16160797", "Robert Galbraith");

            var authors = Series.All();

            authors.Should().NotBeNullOrEmpty();
            authors.Should().Contain(v => v.ForeignSeriesId == "14586394");
            authors.Should().Contain(v => v.ForeignSeriesId == "383606");
        }

        [Test]
        [Order(2)]
        public void get_author_by_id()
        {
            var author = EnsureSeries("14586394", "43765115", "Andrew Hunter Murray");

            var result = Series.Get(author.Id);

            result.ForeignSeriesId.Should().Be("14586394");
        }

        [Test]
        public void get_author_by_unknown_id_should_return_404()
        {
            var result = Series.InvalidGet(1000000);
        }

        [Test]
        [Order(2)]
        public void update_author_profile_id()
        {
            var author = EnsureSeries("14586394", "43765115", "Andrew Hunter Murray");

            var profileId = 1;
            if (author.QualityProfileId == profileId)
            {
                profileId = 2;
            }

            author.QualityProfileId = profileId;

            var result = Series.Put(author);

            Series.Get(author.Id).QualityProfileId.Should().Be(profileId);
        }

        [Test]
        [Order(3)]
        public void update_author_monitored()
        {
            var author = EnsureSeries("14586394", "43765115", "Andrew Hunter Murray", false);

            author.Monitored.Should().BeFalse();

            author.Monitored = true;

            var result = Series.Put(author);

            result.Monitored.Should().BeTrue();
        }

        [Test]
        [Order(3)]
        public void update_author_tags()
        {
            var author = EnsureSeries("14586394", "43765115", "Andrew Hunter Murray");
            var tag = EnsureTag("abc");

            if (author.Tags.Contains(tag.Id))
            {
                author.Tags.Remove(tag.Id);

                var result = Series.Put(author);
                Series.Get(author.Id).Tags.Should().NotContain(tag.Id);
            }
            else
            {
                author.Tags.Add(tag.Id);

                var result = Series.Put(author);
                Series.Get(author.Id).Tags.Should().Contain(tag.Id);
            }
        }

        [Test]
        [Order(4)]
        public void delete_author()
        {
            var author = EnsureSeries("14586394", "43765115", "Andrew Hunter Murray");

            Series.Get(author.Id).Should().NotBeNull();

            Series.Delete(author.Id);

            Series.All().Should().NotContain(v => v.ForeignSeriesId == "14586394");
        }
    }
}
