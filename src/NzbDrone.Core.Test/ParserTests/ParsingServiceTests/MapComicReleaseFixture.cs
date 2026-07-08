using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    [TestFixture]
    public class MapComicReleaseFixture : CoreTest<ParsingService>
    {
        [Test]
        public void should_carry_parsed_year_into_series_title_info()
        {
            var parsed = new ParsedComicInfo
            {
                SeriesTitle = "Green Lantern Corps",
                IssueNumber = 18,
                Year = 2026
            };

            var remoteIssue = Subject.MapComicRelease(parsed);

            remoteIssue.ParsedIssueInfo.SeriesTitleInfo.Year.Should().Be(2026);
            remoteIssue.ParsedIssueInfo.SeriesTitleInfo.Title.Should().Be("Green Lantern Corps");
        }

        [Test]
        public void should_leave_year_zero_when_release_has_no_year()
        {
            var parsed = new ParsedComicInfo
            {
                SeriesTitle = "Green Lantern Corps",
                IssueNumber = 18
            };

            var remoteIssue = Subject.MapComicRelease(parsed);

            remoteIssue.ParsedIssueInfo.SeriesTitleInfo.Year.Should().Be(0);
        }
    }
}
