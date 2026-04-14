using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ComicParserFixture : CoreTest
    {
        // -------------------------------------------------------------------
        // Standard issues
        // -------------------------------------------------------------------
        [TestCase("Batman 2016 001 (2016) (Digital) (Zone-Empire).cbz", "Batman", 1f, 2016)]
        [TestCase("Amazing Spider-Man v5 015 (2022) (Digital) (Shan-Empire).cbr", "Amazing Spider-Man", 15f, 2022)]
        [TestCase("Saga 066 (2024) (digital) (Son of Ultron-Empire).cbz", "Saga", 66f, 2024)]
        [TestCase("Spider-Man 2099 003 (2020).cbz", "Spider-Man 2099", 3f, 2020)]
        [TestCase("100 Bullets 050 (2004).cbz", "100 Bullets", 50f, 2004)]
        public void should_parse_standard_issue(string releaseTitle, string expectedSeries, float expectedIssue, int expectedYear)
        {
            var result = ComicParser.ParseRelease(releaseTitle);

            result.Should().NotBeNull();
            result.SeriesTitle.Should().Be(expectedSeries);
            result.IssueNumber.Should().Be(expectedIssue);
            result.Year.Should().Be(expectedYear);
            result.IssueType.Should().Be(IssueType.Standard);
        }

        // -------------------------------------------------------------------
        // Issue type detection
        // -------------------------------------------------------------------
        [Test]
        public void should_detect_annual()
        {
            var result = ComicParser.ParseRelease("X-Men Annual 001 (2023).cbz");

            result.Should().NotBeNull();
            result.IssueType.Should().Be(IssueType.Annual);
            result.IssueNumber.Should().Be(1f);
            result.Year.Should().Be(2023);
        }

        [Test]
        public void should_detect_tpb()
        {
            var result = ComicParser.ParseRelease("Batman TPB Vol 03 - Death of the Family (2013).cbz");

            result.Should().NotBeNull();
            result.IssueType.Should().Be(IssueType.TPB);
            result.VolumeNumber.Should().Be(3);
            result.Year.Should().Be(2013);
            result.IssueNumber.Should().BeNull("TPBs should not have an issue number");
        }

        [Test]
        public void should_detect_tpb_with_vol_dot()
        {
            var result = ComicParser.ParseRelease("The Walking Dead Vol. 01 - Days Gone Bye (2004).cbz");

            result.Should().NotBeNull();
            result.VolumeNumber.Should().Be(1);
            result.Year.Should().Be(2004);
        }

        // -------------------------------------------------------------------
        // Limited series
        // -------------------------------------------------------------------
        [Test]
        public void should_parse_limited_series()
        {
            var result = ComicParser.ParseRelease("Batman - The Dark Knight Returns 01 (of 04) (1986).cbz");

            result.Should().NotBeNull();
            result.TotalIssues.Should().Be(4);
            result.Year.Should().Be(1986);
        }

        // -------------------------------------------------------------------
        // Format detection
        // -------------------------------------------------------------------
        [TestCase("Something.cbz", ComicFormat.CBZ)]
        [TestCase("Something.cbr", ComicFormat.CBR)]
        [TestCase("Something.cb7", ComicFormat.CB7)]
        [TestCase("Something.pdf", ComicFormat.PDF)]
        [TestCase("Something.epub", ComicFormat.EPUB)]
        [TestCase("Something", ComicFormat.Unknown)]
        public void should_detect_format(string releaseTitle, ComicFormat expectedFormat)
        {
            var result = ComicParser.ParseRelease(releaseTitle);

            result.Should().NotBeNull();
            result.Format.Should().Be(expectedFormat);
        }

        // -------------------------------------------------------------------
        // Volume number extraction
        // -------------------------------------------------------------------
        [TestCase("Amazing Spider-Man v5 015 (2022) (Digital) (Shan-Empire).cbr", 5)]
        [TestCase("Batman TPB Vol 03 - Death of the Family (2013).cbz", 3)]
        [TestCase("The Walking Dead Vol. 01 - Days Gone Bye (2004).cbz", 1)]
        public void should_extract_volume_number(string releaseTitle, int expectedVolume)
        {
            var result = ComicParser.ParseRelease(releaseTitle);

            result.Should().NotBeNull();
            result.VolumeNumber.Should().Be(expectedVolume);
        }

        // -------------------------------------------------------------------
        // Source extraction
        // -------------------------------------------------------------------
        [TestCase("Batman 2016 001 (2016) (Digital) (Zone-Empire).cbz", "Digital")]
        [TestCase("Saga 066 (2024) (digital) (Son of Ultron-Empire).cbz", "digital")]
        public void should_extract_source(string releaseTitle, string expectedSource)
        {
            var result = ComicParser.ParseRelease(releaseTitle);

            result.Should().NotBeNull();
            result.Source.Should().Be(expectedSource);
        }

        // -------------------------------------------------------------------
        // Release group extraction
        // -------------------------------------------------------------------
        [TestCase("Batman 2016 001 (2016) (Digital) (Zone-Empire).cbz", "Zone-Empire")]
        [TestCase("Amazing Spider-Man v5 015 (2022) (Digital) (Shan-Empire).cbr", "Shan-Empire")]
        [TestCase("Saga 066 (2024) (digital) (Son of Ultron-Empire).cbz", "Son of Ultron-Empire")]
        public void should_extract_release_group(string releaseTitle, string expectedGroup)
        {
            var result = ComicParser.ParseRelease(releaseTitle);

            result.Should().NotBeNull();
            result.ReleaseGroup.Should().Be(expectedGroup);
        }

        // -------------------------------------------------------------------
        // Year extraction
        // -------------------------------------------------------------------
        [TestCase("Batman 2016 001 (2016) (Digital) (Zone-Empire).cbz", 2016)]
        [TestCase("Batman - The Dark Knight Returns 01 (of 04) (1986).cbz", 1986)]
        [TestCase("100 Bullets 050 (2004).cbz", 2004)]
        public void should_extract_year(string releaseTitle, int expectedYear)
        {
            var result = ComicParser.ParseRelease(releaseTitle);

            result.Should().NotBeNull();
            result.Year.Should().Be(expectedYear);
        }

        // -------------------------------------------------------------------
        // Null/empty input
        // -------------------------------------------------------------------
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void should_return_null_for_empty_input(string releaseTitle)
        {
            var result = ComicParser.ParseRelease(releaseTitle);

            result.Should().BeNull();
        }

        // -------------------------------------------------------------------
        // Series title extraction for annuals (should strip "Annual" from title)
        // -------------------------------------------------------------------
        [Test]
        public void should_strip_annual_from_series_title()
        {
            var result = ComicParser.ParseRelease("X-Men Annual 001 (2023).cbz");

            result.Should().NotBeNull();
            result.SeriesTitle.Should().Be("X-Men");
        }

        // -------------------------------------------------------------------
        // Series title extraction for TPBs (should strip TPB/Vol markers)
        // -------------------------------------------------------------------
        [Test]
        public void should_extract_series_title_for_tpb()
        {
            var result = ComicParser.ParseRelease("Batman TPB Vol 03 - Death of the Family (2013).cbz");

            result.Should().NotBeNull();
            result.SeriesTitle.Should().Be("Batman - Death of the Family");
        }

        // -------------------------------------------------------------------
        // Quality is set based on format
        // -------------------------------------------------------------------
        [Test]
        public void should_set_quality_from_format()
        {
            var result = ComicParser.ParseRelease("Batman 2016 001 (2016) (Digital) (Zone-Empire).cbz");

            result.Should().NotBeNull();
            result.Quality.Should().NotBeNull();
            result.Quality.Quality.Should().Be(Quality.CBZ);
        }

        // -------------------------------------------------------------------
        // ReleaseTitle is preserved
        // -------------------------------------------------------------------
        [Test]
        public void should_preserve_original_release_title()
        {
            const string title = "Amazing Spider-Man v5 015 (2022) (Digital) (Shan-Empire).cbr";
            var result = ComicParser.ParseRelease(title);

            result.Should().NotBeNull();
            result.ReleaseTitle.Should().Be(title);
        }
    }
}
