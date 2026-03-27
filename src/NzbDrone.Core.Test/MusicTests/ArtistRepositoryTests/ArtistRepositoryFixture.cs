using System;
using System.Collections.Generic;
using System.Data.SQLite;
using FizzWare.NBuilder;
using FluentAssertions;
using Npgsql;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.SeriesRepositoryTests
{
    [TestFixture]

    public class SeriesRepositoryFixture : DbTest<SeriesRepository, Series>
    {
        private SeriesRepository _authorRepo;
        private SeriesMetadataRepository _authorMetadataRepo;

        [SetUp]
        public void Setup()
        {
            _authorRepo = Mocker.Resolve<SeriesRepository>();
            _authorMetadataRepo = Mocker.Resolve<SeriesMetadataRepository>();
        }

        private void AddSeries(string name, string foreignId, List<string> oldIds = null)
        {
            if (oldIds == null)
            {
                oldIds = new List<string>();
            }

            var metadata = Builder<SeriesMetadata>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.Name = name)
                .With(a => a.TitleSlug = foreignId)
                .BuildNew();

            var author = Builder<Series>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.Metadata = metadata)
                .With(a => a.CleanName = Parser.Parser.CleanSeriesName(name))
                .With(a => a.ForeignSeriesId = foreignId)
                .BuildNew();

            _authorMetadataRepo.Insert(metadata);
            author.SeriesMetadataId = metadata.Id;
            _authorRepo.Insert(author);
        }

        private void GivenSeriess()
        {
            AddSeries("The Black Eyed Peas", "d5be5333-4171-427e-8e12-732087c6b78e");
            AddSeries("The Black Keys", "d15721d8-56b4-453d-b506-fc915b14cba2", new List<string> { "6f2ed437-825c-4cea-bb58-bf7688c6317a" });
        }

        [Test]
        public void should_lazyload_profiles()
        {
            var profile = new QualityProfile
            {
                Items = Qualities.QualityFixture.GetDefaultQualities(Quality.CBZ_HD, Quality.CBR, Quality.CBR),

                Cutoff = Quality.CBZ_HD.Id,
                Name = "TestProfile"
            };

            Mocker.Resolve<QualityProfileRepository>().Insert(profile);

            var author = Builder<Series>.CreateNew().BuildNew();
            author.QualityProfileId = profile.Id;

            Subject.Insert(author);

            StoredModel.QualityProfile.Should().NotBeNull();
        }

        [TestCase("The Black Eyed Peas")]
        [TestCase("The Black Keys")]
        public void should_find_author_in_db_by_name(string name)
        {
            GivenSeriess();
            var author = _authorRepo.FindByName(Parser.Parser.CleanSeriesName(name));

            author.Should().NotBeNull();
            author.Name.Should().Be(name);
        }

        [Test]
        public void should_find_author_in_by_id()
        {
            GivenSeriess();
            var author = _authorRepo.FindById("d5be5333-4171-427e-8e12-732087c6b78e");

            author.Should().NotBeNull();
            author.ForeignSeriesId.Should().Be("d5be5333-4171-427e-8e12-732087c6b78e");
        }

        [Test]
        public void should_not_find_author_if_multiple_authors_have_same_name()
        {
            GivenSeriess();

            var name = "Alice Cooper";
            AddSeries(name, "ee58c59f-8e7f-4430-b8ca-236c4d3745ae");
            AddSeries(name, "4d7928cd-7ed2-4282-8c29-c0c9f966f1bd");

            _authorRepo.All().Should().HaveCount(4);

            var author = _authorRepo.FindByName(Parser.Parser.CleanSeriesName(name));
            author.Should().BeNull();
        }

        [Test]
        public void should_throw_sql_exception_adding_duplicate_author()
        {
            var name = "test";
            var metadata = Builder<SeriesMetadata>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.Name = name)
                .BuildNew();

            var author1 = Builder<Series>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.Metadata = metadata)
                .With(a => a.CleanName = Parser.Parser.CleanSeriesName(name))
                .BuildNew();

            var author2 = author1.JsonClone();
            author2.Metadata = metadata;

            _authorMetadataRepo.Insert(metadata);
            _authorRepo.Insert(author1);

            Action insertDupe = () => _authorRepo.Insert(author2);
            if (Db.DatabaseType == DatabaseType.PostgreSQL)
            {
                insertDupe.Should().Throw<PostgresException>();
            }
            else
            {
                insertDupe.Should().Throw<SQLiteException>();
            }
        }
    }
}
