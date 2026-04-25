using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class QualityParserFixture : CoreTest
    {
        public static object[] SelfQualityParserCases =
        {
            new object[] { Quality.CBR },
            new object[] { Quality.CBZ_HD },
            new object[] { Quality.EPUB },
            new object[] { Quality.CBZ },
            new object[] { Quality.PDF },
            new object[] { Quality.CB7 }
        };

        [Test]
        [TestCaseSource(nameof(SelfQualityParserCases))]
        public void parsing_our_own_quality_enum_name(Quality quality)
        {
            var fileName = string.Format("Some issue [{0}]", quality.Name);
            var result = QualityParser.ParseQuality(fileName);
            result.Quality.Should().Be(quality);
        }

        [Test]
        public void should_parse_null_quality_description_as_unknown()
        {
            QualityParser.ParseCodec(null, null).Should().Be(Codec.Unknown);
        }

        [TestCase("Series Title - Issue Title 2017 REPACK PDF aAF", true)]
        [TestCase("Series Title - Issue Title 2017 RERIP PDF aAF", true)]
        [TestCase("Series Title - Issue Title 2017 PROPER PDF aAF", false)]
        public void should_be_able_to_parse_repack(string title, bool isRepack)
        {
            var result = QualityParser.ParseQuality(title);
            result.Revision.Version.Should().Be(2);
            result.Revision.IsRepack.Should().Be(isRepack);
        }

        public static object[] ExtensionQualityCases =
        {
            new object[] { "Batman 001 (2016).cbz", Quality.CBZ },
            new object[] { "Batman 001 (2016).cbr", Quality.CBR },
            new object[] { "Batman 001 (2016).cb7", Quality.CB7 },
            new object[] { "Batman 001 (2016).pdf", Quality.PDF },
        };

        [Test]
        [TestCaseSource(nameof(ExtensionQualityCases))]
        public void should_parse_quality_from_extension(string title, Quality expected)
        {
            var result = QualityParser.ParseQuality(title);
            result.Quality.Should().Be(expected);
        }

        [TestCase("Some random title without extension")]
        [TestCase("Unknown format file")]
        public void should_parse_unknown_quality(string title)
        {
            var result = QualityParser.ParseQuality(title);
            result.Quality.Should().Be(Quality.Unknown);
        }

        [TestCase("Some issue [PDF]")]
        public void should_parse_quality_from_name(string title)
        {
            QualityParser.ParseQuality(title).QualityDetectionSource.Should().Be(QualityDetectionSource.Name);
        }

        private void ParseAndVerifyQuality(string name, string desc, int bitrate, Quality quality, int sampleSize = 0)
        {
            var result = QualityParser.ParseQuality(name);
            result.Quality.Should().Be(quality);
        }
    }
}
