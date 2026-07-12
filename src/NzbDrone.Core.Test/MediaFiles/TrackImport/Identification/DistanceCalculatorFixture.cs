using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.IssueImport.Identification
{
    [TestFixture]
    public class DistanceCalculatorFixture : TestBase
    {
        private static List<LocalIssue> GivenLocalTracks(string seriesIndex, string publisher = null)
        {
            return new List<LocalIssue>
            {
                new LocalIssue
                {
                    FileTagInfo = new ParsedFileTagInfo
                    {
                        Series = new List<string> { "Saga" },
                        SeriesIndex = seriesIndex,
                        Publisher = publisher
                    }
                }
            };
        }

        private static Issue GivenDbIssue(string issueNumber, string publisherName = null)
        {
            var metadata = new SeriesMetadata
            {
                Name = "Saga",
                PublisherId = publisherName != null ? 1 : null,
                Publisher = publisherName != null ? new Publisher { Name = publisherName } : null
            };

            return new Issue
            {
                IssueNumber = issueNumber,
                Title = string.Empty,
                SeriesMetadata = metadata
            };
        }

        [TestCase("003", "3")]
        [TestCase("3", "3")]
        [TestCase("0.50", "0.5")]
        public void issue_number_padding_should_not_add_distance(string fileNumber, string dbNumber)
        {
            var dist = DistanceCalculator.IssueDistance(GivenLocalTracks(fileNumber), GivenDbIssue(dbNumber));

            dist.NormalizedDistance().Should().Be(0.0);
        }

        [Test]
        public void publisher_should_compare_against_publisher_name_not_series_name()
        {
            // "Image" vs series name "Saga" used to incur a guaranteed penalty
            var dist = DistanceCalculator.IssueDistance(GivenLocalTracks("3", "Image"), GivenDbIssue("3", "Image"));

            dist.NormalizedDistance().Should().Be(0.0);
        }

        [Test]
        public void publisher_should_not_be_compared_when_db_publisher_unknown()
        {
            var dist = DistanceCalculator.IssueDistance(GivenLocalTracks("3", "Image"), GivenDbIssue("3"));

            dist.NormalizedDistance().Should().Be(0.0);
        }

        [Test]
        public void mismatched_publisher_should_add_distance()
        {
            var dist = DistanceCalculator.IssueDistance(GivenLocalTracks("3", "Marvel"), GivenDbIssue("3", "Image"));

            dist.NormalizedDistance().Should().BeGreaterThan(0.0);
        }

        [TestCase("DC", "DC Comics")]
        [TestCase("DC Comics", "DC")]
        [TestCase("IDW", "IDW Publishing")]
        [TestCase("Marvel", "Marvel Comics")]
        [TestCase("BOOM! Studios", "Boom Studios")]
        public void publisher_alias_forms_should_not_add_distance(string filePublisher, string dbPublisher)
        {
            var dist = DistanceCalculator.IssueDistance(GivenLocalTracks("3", filePublisher), GivenDbIssue("3", dbPublisher));

            dist.NormalizedDistance().Should().Be(0.0);
        }

        [TestCase("")]
        [TestCase("#18")]
        [TestCase("018")]
        public void placeholder_local_title_should_not_be_compared_against_db_title(string localTitle)
        {
            // "#18" vs "The God Game Conclusion" is a guaranteed max penalty
            // that sank correct matches below the import threshold
            var tracks = GivenLocalTracks("18");
            tracks[0].FileTagInfo.IssueTitle = localTitle;

            var issue = GivenDbIssue("18");
            issue.Title = "The God Game Conclusion";

            var dist = DistanceCalculator.IssueDistance(tracks, issue);

            dist.NormalizedDistance().Should().Be(0.0);
        }

        [Test]
        public void real_local_title_should_still_be_compared()
        {
            var tracks = GivenLocalTracks("18");
            tracks[0].FileTagInfo.IssueTitle = "A Completely Different Story";

            var issue = GivenDbIssue("18");
            issue.Title = "The God Game Conclusion";

            var dist = DistanceCalculator.IssueDistance(tracks, issue);

            dist.NormalizedDistance().Should().BeGreaterThan(0.0);
        }

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
