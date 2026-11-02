using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    [TestFixture]
    public class AnnualVolumeResolutionFixture : CoreTest<ParsingService>
    {
        private Series _parent;
        private Series _annual;

        [SetUp]
        public void Setup()
        {
            _parent = new Series { Id = 319, Name = "Absolute Batman", CleanName = "absolutebatman" };
            _annual = new Series { Id = 1423, Name = "Absolute Batman 2025 Annual", CleanName = "absolutebatman2025annual" };

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindByName(It.IsAny<string>()))
                  .Returns((Series)null);

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindByName("Absolute Batman"))
                  .Returns(_parent);

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindByName("Absolute Batman 2025 Annual"))
                  .Returns(_annual);
        }

        private static ParsedComicInfo Parsed()
        {
            return ComicParser.ParseRelease("Absolute Batman 2025 Annual #1 (2025)");
        }

        [Test]
        public void should_resolve_the_annual_volume_rather_than_the_parent_series()
        {
            // The exact-title release was being mapped to the parent series and
            // then rejected as "Wrong series" by the search specifications
            Subject.GetSeriesForComicRelease(Parsed()).Should().Be(_annual);
        }

        [Test]
        public void should_fall_back_to_the_parent_when_no_annual_volume_exists()
        {
            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.FindByName("Absolute Batman 2025 Annual"))
                  .Returns((Series)null);

            Subject.GetSeriesForComicRelease(Parsed()).Should().Be(_parent);
        }

        [Test]
        public void should_accept_the_searched_annual_volume_from_criteria()
        {
            var criteria = new IssueSearchCriteria { Series = _annual, Issues = new List<Issue>() };

            Subject.GetSeriesForComicRelease(Parsed(), criteria).Should().Be(_annual);
        }

        [Test]
        public void should_not_claim_a_release_for_an_unrelated_searched_series()
        {
            var other = new Series { Id = 777, Name = "Nightwing", CleanName = "nightwing" };
            var criteria = new IssueSearchCriteria { Series = other, Issues = new List<Issue>() };

            Subject.GetSeriesForComicRelease(Parsed(), criteria).Should().Be(_annual);
        }

        [Test]
        public void should_still_resolve_a_release_with_no_candidates()
        {
            var parsed = new ParsedComicInfo { SeriesTitle = "Absolute Batman", IssueNumber = 1 };

            Subject.GetSeriesForComicRelease(parsed).Should().Be(_parent);
        }
    }
}
