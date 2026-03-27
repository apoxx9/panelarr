using System.Collections;
using System.Linq;
using System.Reflection;
using AutoFixture;
using Equ;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class EntityFixture : LoggingTest
    {
        private Fixture _fixture = new Fixture();

        private static bool IsNotMarkedAsIgnore(PropertyInfo propertyInfo)
        {
            return !propertyInfo.GetCustomAttributes(typeof(MemberwiseEqualityIgnoreAttribute), true).Any();
        }

        public class EqualityPropertySource<T>
        {
            public static IEnumerable TestCases
            {
                get
                {
                    foreach (var property in typeof(T).GetProperties().Where(x => x.CanRead && x.CanWrite && IsNotMarkedAsIgnore(x)))
                    {
                        yield return new TestCaseData(property).SetName($"{{m}}_{property.Name}");
                    }
                }
            }
        }

        public class IgnoredPropertySource<T>
        {
            public static IEnumerable TestCases
            {
                get
                {
                    foreach (var property in typeof(T).GetProperties().Where(x => x.CanRead && x.CanWrite && !IsNotMarkedAsIgnore(x)))
                    {
                        yield return new TestCaseData(property).SetName($"{{m}}_{property.Name}");
                    }
                }
            }
        }

        [Test]
        public void two_equivalent_author_metadata_should_be_equal()
        {
            var item1 = _fixture.Create<SeriesMetadata>();
            var item2 = item1.JsonClone();

            item1.Should().NotBeSameAs(item2);
            item1.Should().Be(item2);
        }

        [Test]
        [TestCaseSource(typeof(EqualityPropertySource<SeriesMetadata>), "TestCases")]
        public void two_different_author_metadata_should_not_be_equal(PropertyInfo prop)
        {
            var item1 = _fixture.Create<SeriesMetadata>();
            var item2 = item1.JsonClone();
            var different = _fixture.Create<SeriesMetadata>();

            // make item2 different in the property under consideration
            var differentEntry = prop.GetValue(different);
            prop.SetValue(item2, differentEntry);

            item1.Should().NotBeSameAs(item2);
            item1.Should().NotBe(item2);
        }

        [Test]
        public void metadata_and_db_fields_should_replicate_author_metadata()
        {
            var item1 = _fixture.Create<SeriesMetadata>();
            var item2 = _fixture.Create<SeriesMetadata>();

            item1.Should().NotBe(item2);

            item1.UseMetadataFrom(item2);
            item1.UseDbFieldsFrom(item2);
            item1.Should().Be(item2);
        }

        private Issue GivenBook()
        {
            return _fixture.Build<Issue>()
                .Without(x => x.SeriesMetadata)
                .Without(x => x.Series)
                .Without(x => x.SeriesId)
                .Create();
        }

        [Test]
        public void two_equivalent_books_should_be_equal()
        {
            var item1 = GivenBook();
            var item2 = item1.JsonClone();

            item1.Should().NotBeSameAs(item2);
            item1.Should().Be(item2);
        }

        [Test]
        [TestCaseSource(typeof(EqualityPropertySource<Issue>), "TestCases")]
        public void two_different_books_should_not_be_equal(PropertyInfo prop)
        {
            var item1 = GivenBook();
            var item2 = item1.JsonClone();
            var different = GivenBook();

            // make item2 different in the property under consideration
            if (prop.PropertyType == typeof(bool))
            {
                prop.SetValue(item2, !(bool)prop.GetValue(item1));
            }
            else
            {
                prop.SetValue(item2, prop.GetValue(different));
            }

            item1.Should().NotBeSameAs(item2);
            item1.Should().NotBe(item2);
        }

        [Test]
        public void metadata_and_db_fields_should_replicate_book()
        {
            var item1 = GivenBook();
            var item2 = GivenBook();

            item1.Should().NotBe(item2);

            item1.UseMetadataFrom(item2);
            item1.UseDbFieldsFrom(item2);
            item1.Should().Be(item2);
        }

        private Edition GivenEdition()
        {
            return _fixture.Build<Edition>()
                .Without(x => x.Issue)
                .Without(x => x.ComicFiles)
                .Create();
        }

        [Test]
        public void two_equivalent_editions_should_be_equal()
        {
            var item1 = GivenEdition();
            var item2 = item1.JsonClone();

            item1.Should().NotBeSameAs(item2);
            item1.Should().Be(item2);
        }

        [Test]
        [TestCaseSource(typeof(EqualityPropertySource<Edition>), "TestCases")]
        public void two_different_editions_should_not_be_equal(PropertyInfo prop)
        {
            var item1 = GivenEdition();
            var item2 = item1.JsonClone();
            var different = GivenEdition();

            // make item2 different in the property under consideration
            if (prop.PropertyType == typeof(bool))
            {
                prop.SetValue(item2, !(bool)prop.GetValue(item1));
            }
            else
            {
                prop.SetValue(item2, prop.GetValue(different));
            }

            item1.Should().NotBeSameAs(item2);
            item1.Should().NotBe(item2);
        }

        [Test]
        public void metadata_and_db_fields_should_replicate_edition()
        {
            var item1 = GivenEdition();
            var item2 = GivenEdition();

            item1.Should().NotBe(item2);

            item1.UseMetadataFrom(item2);
            item1.UseDbFieldsFrom(item2);
            item1.Should().Be(item2);
        }

        private Series GivenSeries()
        {
            return _fixture.Build<Series>()
                .With(x => x.Metadata, new LazyLoaded<SeriesMetadata>(_fixture.Create<SeriesMetadata>()))
                .Without(x => x.QualityProfile)
                .Without(x => x.Books)
                .Without(x => x.Name)
                .Without(x => x.ForeignSeriesId)
                .Create();
        }

        [Test]
        public void two_equivalent_authors_should_be_equal()
        {
            var item1 = GivenSeries();
            var item2 = item1.JsonClone();

            item1.Should().NotBeSameAs(item2);
            item1.Should().Be(item2);
        }

        [Test]
        [TestCaseSource(typeof(EqualityPropertySource<Series>), "TestCases")]
        public void two_different_authors_should_not_be_equal(PropertyInfo prop)
        {
            var item1 = GivenSeries();
            var item2 = item1.JsonClone();
            var different = GivenSeries();

            // make item2 different in the property under consideration
            if (prop.PropertyType == typeof(bool))
            {
                prop.SetValue(item2, !(bool)prop.GetValue(item1));
            }
            else
            {
                prop.SetValue(item2, prop.GetValue(different));
            }

            item1.Should().NotBeSameAs(item2);
            item1.Should().NotBe(item2);
        }

        [Test]
        public void metadata_and_db_fields_should_replicate_author()
        {
            var item1 = GivenSeries();
            var item2 = GivenSeries();

            item1.Should().NotBe(item2);

            item1.UseMetadataFrom(item2);
            item1.UseDbFieldsFrom(item2);
            item1.Should().Be(item2);
        }
    }
}
