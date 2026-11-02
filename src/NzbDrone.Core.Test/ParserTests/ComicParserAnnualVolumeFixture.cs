using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class ComicParserAnnualVolumeFixture : CoreTest
    {
        [TestCase("Absolute Batman 2025 Annual #1 (2025)", "Absolute Batman 2025 Annual")]
        [TestCase("Absolute Wonder Woman 2026 Annual #1 (2026)", "Absolute Wonder Woman 2026 Annual")]
        [TestCase("Mighty Morphin Power Rangers 2016 Annual #1 (2016)", "Mighty Morphin Power Rangers 2016 Annual")]
        public void should_offer_the_unstripped_name_first_for_an_annual_volume(string release, string expected)
        {
            // ComicVine publishes these annuals as their own volumes, named for
            // the year - stripping both "Annual" and the year collapsed them
            // onto the parent series and the release was rejected as the wrong
            // series, permanently, even on an exact title match.
            var result = ComicParser.ParseRelease(release);

            result.SeriesTitleCandidates.Should().NotBeEmpty();
            result.SeriesTitleCandidates[0].Should().Be(expected);
        }

        [Test]
        public void should_still_offer_the_fully_stripped_name_as_a_candidate()
        {
            var result = ComicParser.ParseRelease("Absolute Batman 2025 Annual #1 (2025)");

            result.SeriesTitle.Should().Be("Absolute Batman");
            result.SeriesTitleCandidates.Should().ContainInOrder(
                "Absolute Batman 2025 Annual",
                "Absolute Batman Annual",
                "Absolute Batman 2025",
                "Absolute Batman");
        }

        [Test]
        public void should_keep_a_year_that_is_part_of_the_series_name()
        {
            var result = ComicParser.ParseRelease("Spider-Man 2099 #1 (2025)");

            result.SeriesTitle.Should().Be("Spider-Man 2099");
            result.SeriesTitleCandidates.Should().Contain("Spider-Man 2099");
        }

        [Test]
        public void should_not_invent_candidates_for_an_ordinary_release()
        {
            var result = ComicParser.ParseRelease("Absolute Batman #1 (2025)");

            result.SeriesTitle.Should().Be("Absolute Batman");
            result.SeriesTitleCandidates.Should().Equal("Absolute Batman");
        }

        [Test]
        public void should_offer_an_annual_without_a_year_before_the_parent()
        {
            var result = ComicParser.ParseRelease("Detective Comics Annual #1 (2011)");

            result.SeriesTitleCandidates.Should().ContainInOrder("Detective Comics Annual", "Detective Comics");
        }
    }
}
