using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.IssueImport.Identification
{
    [TestFixture]
    public class DistanceCalculatorFixture : TestBase
    {
        [Test]
        public void should_reverse_single_reversed_author()
        {
            var input = new List<string> { "Last, First" };
            var allSeries = DistanceCalculator.GetSeriesVariants(input);

            allSeries.Should().Contain("First Last");
        }

        [Test]
        public void should_reverse_two_reversed_author()
        {
            var input = new List<string>
            {
                "Last, First",
                "Last2, First2"
            };

            var allSeries = DistanceCalculator.GetSeriesVariants(input);

            allSeries.Should().HaveCount(4);
            allSeries.Should().Contain("First Last");
            allSeries.Should().Contain("First2 Last2");
            allSeries.Should().Contain("Last, First");
            allSeries.Should().Contain("Last2, First2");
        }

        [Test]
        public void should_not_reverse_single_series()
        {
            var input = new List<string> { "First Last" };
            var allSeries = DistanceCalculator.GetSeriesVariants(input);

            allSeries.Should().HaveCount(1);
            allSeries.Should().Contain("First Last");
        }

        [TestCase("First1 Last1, First2 Last2", "First1 Last1", "First2 Last2")]
        [TestCase("First1 Last1; First2 Last2", "First1 Last1", "First2 Last2")]
        [TestCase("First1 Last1 & First2 Last2", "First1 Last1", "First2 Last2")]
        [TestCase("First1 Last1 / First2 Last2", "First1 Last1", "First2 Last2")]
        [TestCase("First1 Last1 and First2 Last2", "First1 Last1", "First2 Last2")]
        public void should_split_concatenated_series(string inputString, string first, string second)
        {
            var input = new List<string> { inputString };
            var allSeries = DistanceCalculator.GetSeriesVariants(input);

            allSeries.Should().Contain(inputString);
            allSeries.Should().Contain(first);
            allSeries.Should().Contain(second);
            allSeries.Should().HaveCount(3);
        }

        [Test]
        public void should_split_concatenated_with_trailing_and()
        {
            var inputString = "First Last, First2 Last2 & First3 Last3";
            var input = new List<string> { inputString };
            var allSeries = DistanceCalculator.GetSeriesVariants(input);

            allSeries.Should().Contain(inputString);
            allSeries.Should().Contain("First Last");
            allSeries.Should().Contain("First2 Last2");
            allSeries.Should().Contain("First3 Last3");
            allSeries.Should().HaveCount(4);
        }

        [Test]
        public void should_not_split_if_multiple_input()
        {
            var input = new List<string>
            {
                "First Last",
                "Second Third, Fourth Fifth"
            };

            var allSeries = DistanceCalculator.GetSeriesVariants(input);

            allSeries.Should().HaveCount(2);
            allSeries.Should().Contain("First Last");
            allSeries.Should().Contain("Second Third, Fourth Fifth");
        }
    }
}
