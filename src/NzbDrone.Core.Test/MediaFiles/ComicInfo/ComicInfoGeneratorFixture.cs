using System.IO;
using System.Text;
using System.Xml;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.ComicInfo;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaFiles.ComicInfo
{
    [TestFixture]
    public class ComicInfoGeneratorFixture : CoreTest<ComicInfoGenerator>
    {
        private Issue _issue;
        private SeriesMetadata _seriesMetadata;
        private Publisher _publisher;

        [SetUp]
        public void Setup()
        {
            _issue = new Issue
            {
                IssueNumber = "1",
                Title = "The World's Greatest Comic Magazine"
            };

            _seriesMetadata = new SeriesMetadata
            {
                Name = "Fantastic Four Epic Collection: The World's Greatest Comic Magazine",
                Year = 2014
            };

            _publisher = new Publisher { Name = "Marvel" };
        }

        [Test]
        public void should_declare_utf8_encoding()
        {
            // The embed writes the returned string as UTF-8 bytes. A utf-16
            // declaration over UTF-8 bytes is rejected by strict parsers
            // (observed live: Kavita ignored every Panelarr-embedded tag).
            var xml = Subject.Generate(_issue, _seriesMetadata, _publisher);

            xml.Should().StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        }

        [Test]
        public void should_be_parseable_from_utf8_bytes_by_a_strict_stream_reader()
        {
            var xml = Subject.Generate(_issue, _seriesMetadata, _publisher);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var doc = new XmlDocument();
            doc.Load(stream);

            doc.DocumentElement.Name.Should().Be("ComicInfo");
            doc.SelectSingleNode("//Series").InnerText.Should().Be(_seriesMetadata.Name);
        }

        [Test]
        public void should_write_web_as_comicvine_issue_page_not_cover_art()
        {
            // Kavita's ComicVine weblink parser throws on CV uploads (image)
            // URLs and drops the file from its library entirely
            _issue.ForeignIssueId = "cv:1123951";
            _issue.CoverArtUrl = "https://comicvine.gamespot.com/a/uploads/original/11/110017/9528005-wwww.jpg";

            var xml = Subject.Generate(_issue, _seriesMetadata, _publisher);

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            doc.SelectSingleNode("//Web").InnerText.Should().Be("https://comicvine.gamespot.com/issue/4000-1123951/");
        }

        [Test]
        public void should_omit_web_without_a_comicvine_issue_id()
        {
            _issue.ForeignIssueId = "metron:1234";
            _issue.CoverArtUrl = "https://comicvine.gamespot.com/a/uploads/original/11/110017/9528005-wwww.jpg";

            var xml = Subject.Generate(_issue, _seriesMetadata, _publisher);

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            doc.SelectSingleNode("//Web").Should().BeNull();
        }

        [Test]
        public void should_write_volume_as_series_year()
        {
            // ComicVine/Mylar convention; Kavita's ComicVine library type
            // groups series by ComicInfo Series + Volume
            var xml = Subject.Generate(_issue, _seriesMetadata, _publisher);

            var doc = new XmlDocument();
            doc.LoadXml(xml);
            doc.SelectSingleNode("//Volume").InnerText.Should().Be("2014");
        }
    }

    [TestFixture]
    public class MetronInfoGeneratorFixture : CoreTest<MetronInfoGenerator>
    {
        [Test]
        public void should_declare_utf8_encoding_and_parse_from_utf8_bytes()
        {
            var issue = new Issue { IssueNumber = "1" };
            var seriesMetadata = new SeriesMetadata { Name = "Saga", Year = 2012 };

            var xml = Subject.Generate(issue, seriesMetadata, new Publisher { Name = "Image" });

            xml.Should().StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            var doc = new XmlDocument();
            doc.Load(stream);
            doc.DocumentElement.Name.Should().Be("MetronInfo");
        }
    }
}
