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
        // Source tags rank a release
        [TestCase("Saga 003 (2012) (Digital) (Zone-Empire)", "Digital")]
        [TestCase("Batman 001 (2016) (Webrip) (The Last Kryptonian-DCP)", "WebRip")]
        [TestCase("Batman 001 (2016) (Web-Rip)", "WebRip")]
        [TestCase("Spawn 001 (1992) (c2c) (Glorith-HD)", "C2C")]
        [TestCase("Spawn 001 (1992) (Cover to Cover)", "C2C")]
        [TestCase("X-Men 100 (1976) (Scan)", "Scan")]
        [TestCase("X-Men 100 (1976) (scanned)", "Scan")]
        [TestCase("X-Men 100 (1976) (Print)", "Scan")]
        public void should_parse_source_quality(string title, string expected)
        {
            QualityParser.ParseQuality(title).Quality.Name.Should().Be(expected);
        }

        // Digital wins regardless of token position
        [TestCase("Saga 003 (Scan) (Digital)")]
        [TestCase("Saga 003 (Digital) (Scan)")]
        public void digital_should_win_over_other_tokens(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Digital);
        }

        // Bare container tokens only tell us it's an archive of unknown source
        [TestCase("Batman 001 [CBZ]")]
        [TestCase("Batman 001 [CBR]")]
        [TestCase("Batman 001 CB7")]
        public void container_tokens_should_parse_as_archive(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Archive);
        }

        [TestCase("Batman 001 (2016).cbz")]
        [TestCase("Batman 001 (2016).cbr")]
        [TestCase("Batman 001 (2016).cb7")]
        public void archive_extensions_should_parse_as_archive(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Archive);
        }

        [TestCase("Batman 001 (2016).pdf", "PDF")]
        [TestCase("Some issue [PDF]", "PDF")]
        [TestCase("Some issue [EPUB]", "EPUB")]
        [TestCase("Some issue [MOBI]", "EPUB")]
        [TestCase("Some issue [AZW3]", "EPUB")]
        public void format_locked_releases_should_parse_to_format_rung(string title, string expected)
        {
            QualityParser.ParseQuality(title).Quality.Name.Should().Be(expected);
        }

        [TestCase("Some random title without extension")]
        [TestCase("Unknown format file")]
        public void should_parse_unknown_quality(string title)
        {
            QualityParser.ParseQuality(title).Quality.Should().Be(Quality.Unknown);
        }

        // Comic fix-releases map onto the revision system
        [TestCase("Saga 003 (2012) (Digital) (f) (Zone-Empire)", 2)]
        [TestCase("Saga 003 (2012) (Digital) (Fixed)", 2)]
        [TestCase("Saga 003 (2012) (Digital) (f2)", 3)]
        public void fixed_markers_should_bump_revision(string title, int expectedVersion)
        {
            var result = QualityParser.ParseQuality(title);
            result.Quality.Should().Be(Quality.Digital);
            result.Revision.Version.Should().Be(expectedVersion);
        }

        [TestCase("Saga 003 (2012) (Digital)")]
        public void no_fix_marker_should_be_v1(string title)
        {
            QualityParser.ParseQuality(title).Revision.Version.Should().Be(1);
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

        [TestCase("Some issue [PDF]")]
        public void should_parse_quality_from_name(string title)
        {
            QualityParser.ParseQuality(title).QualityDetectionSource.Should().Be(QualityDetectionSource.Name);
        }
    }
}
