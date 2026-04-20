using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    [Ignore("Waiting for metadata to be back again", Until = "2026-01-15 00:00:00Z")]
    public class SeriesLookupFixture : IntegrationTest
    {
        [TestCase("Robert Harris", "Robert Harris")]
        [TestCase("Philip W. Errington", "Philip W. Errington")]
        public void lookup_new_author_by_name(string term, string name)
        {
            var series = Series.Lookup(term);

            series.Should().NotBeEmpty();
            series.Should().Contain(c => c.SeriesName == name);
        }

        [Test]
        public void lookup_new_author_by_goodreads_book_id()
        {
            var series = Series.Lookup("edition:2");

            series.Should().NotBeEmpty();
            series.Should().Contain(c => c.SeriesName == "J.K. Rowling");
        }
    }
}
