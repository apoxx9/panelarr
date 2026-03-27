using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MusicTests.SeriesServiceTests
{
    [TestFixture]

    public class FindByNameInexactFixture : CoreTest<SeriesService>
    {
        private List<Series> _authors;

        private Series CreateSeries(string name)
        {
            return Builder<Series>.CreateNew()
                .With(a => a.Name = name)
                .With(a => a.CleanName = Parser.Parser.CleanSeriesName(name))
                .With(a => a.ForeignSeriesId = name)
                .BuildNew();
        }

        [SetUp]
        public void Setup()
        {
            _authors = new List<Series>();
            _authors.Add(CreateSeries("The Black Eyed Peas"));
            _authors.Add(CreateSeries("The Black Keys"));

            Mocker.GetMock<ISeriesRepository>()
                .Setup(s => s.All())
                .Returns(_authors);
        }

        [TestCase("The Black Eyd Peas", "The Black Eyed Peas")]
        [TestCase("The Black eys", "The Black Keys")]
        public void should_find_author_in_db_by_name_inexact(string name, string expected)
        {
            var author = Subject.FindByNameInexact(name);

            author.Should().NotBeNull();
            author.Name.Should().Be(expected);
        }

        [TestCase("The Black Peas")]
        public void should_not_find_author_in_db_by_ambiguous_name(string name)
        {
            var author = Subject.FindByNameInexact(name);

            author.Should().BeNull();
        }
    }
}
