using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Arcs.Cbl;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ArcTests
{
    [TestFixture]
    public class CblFormatFixture : CoreTest
    {
        // Shape taken from a real DieselTech community list (Knightfall).
        private const string SampleCbl = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ReadingList xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
<Name>[DC Comics] Batman - Knightfall</Name>
<NumIssues>2</NumIssues>
<Books>
<Book Series=""Batman: Sword of Azrael"" Number=""1"" Volume=""1992"" Year=""1992"">
<Database Name=""cv"" Series=""9223"" Issue=""107898"" />
</Book>
<Book Series=""Batman"" Number=""484"" Volume=""1940"" Year=""1992"" />
</Books>
<Matchers />
</ReadingList>";

        [Test]
        public void should_parse_a_community_cbl()
        {
            var list = CblFormat.Parse(SampleCbl);

            list.Name.Should().Be("[DC Comics] Batman - Knightfall");
            list.Books.Should().HaveCount(2);

            list.Books[0].Series.Should().Be("Batman: Sword of Azrael");
            list.Books[0].Number.Should().Be("1");
            list.Books[0].Volume.Should().Be("1992");
            list.Books[0].Year.Should().Be("1992");
            list.Books[0].CvVolumeId.Should().Be(9223);
            list.Books[0].CvIssueId.Should().Be(107898);

            list.Books[1].Series.Should().Be("Batman");
            list.Books[1].CvIssueId.Should().BeNull();
        }

        [Test]
        public void should_round_trip_losslessly()
        {
            var original = CblFormat.Parse(SampleCbl);

            var written = CblFormat.Write(original.Name, original.Books);
            var reparsed = CblFormat.Parse(written);

            reparsed.Name.Should().Be(original.Name);
            reparsed.Books.Should().HaveCount(original.Books.Count);

            for (var i = 0; i < original.Books.Count; i++)
            {
                reparsed.Books[i].Series.Should().Be(original.Books[i].Series);
                reparsed.Books[i].Number.Should().Be(original.Books[i].Number);
                reparsed.Books[i].Volume.Should().Be(original.Books[i].Volume);
                reparsed.Books[i].Year.Should().Be(original.Books[i].Year);
                reparsed.Books[i].CvVolumeId.Should().Be(original.Books[i].CvVolumeId);
                reparsed.Books[i].CvIssueId.Should().Be(original.Books[i].CvIssueId);
            }
        }

        [Test]
        public void should_preserve_book_order()
        {
            var books = new List<CblBook>
            {
                new CblBook { Series = "Z Comes First Here", Number = "9" },
                new CblBook { Series = "A Comes Second", Number = "1" }
            };

            var reparsed = CblFormat.Parse(CblFormat.Write("Order Test", books));

            reparsed.Books[0].Series.Should().Be("Z Comes First Here");
            reparsed.Books[1].Series.Should().Be("A Comes Second");
        }

        [Test]
        public void should_reject_non_xml()
        {
            Assert.Throws<InvalidCblFileException>(() => CblFormat.Parse("not xml at all"));
        }

        [Test]
        public void should_reject_wrong_root()
        {
            Assert.Throws<InvalidCblFileException>(() => CblFormat.Parse("<NotAReadingList />"));
        }
    }
}
