using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource.Metron;
using NzbDrone.Core.MetadataSource.Provider;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.Metron
{
    [TestFixture]
    public class MetronMapperFixture : CoreTest<MetronMapper>
    {
        private ProviderSeries GivenSeries()
        {
            return new ProviderSeries
            {
                ForeignSeriesId = "99",
                Name = "Saga",
                Year = 2012
            };
        }

        [TestCase("Ongoing", SeriesStatusType.Continuing)]
        [TestCase("Hiatus", SeriesStatusType.Continuing)]
        [TestCase("Cancelled", SeriesStatusType.Ended)]
        [TestCase("Completed", SeriesStatusType.Ended)]
        [TestCase(null, SeriesStatusType.Continuing)]
        public void should_map_metron_status_display_strings(string status, SeriesStatusType expected)
        {
            var series = GivenSeries();
            series.Status = status;

            var (metadata, _) = Subject.MapSeries(series);

            metadata.Status.Should().Be(expected);
        }

        [TestCase("Single Issue", SeriesType.Single)]
        [TestCase("Limited Series", SeriesType.Limited)]
        [TestCase("Annual Series", SeriesType.Annual)]
        [TestCase("Trade Paperback", SeriesType.TPB)]
        [TestCase("Hardcover", SeriesType.Hardcover)]
        [TestCase("Omnibus", SeriesType.Omnibus)]
        [TestCase("One-Shot", SeriesType.OneShot)]
        [TestCase("Graphic Novel", SeriesType.GraphicNovel)]
        [TestCase("Digital Chapters", SeriesType.Single)]
        public void should_map_metron_series_type_names(string seriesType, SeriesType expected)
        {
            var series = GivenSeries();
            series.SeriesType = seriesType;

            var (metadata, _) = Subject.MapSeries(series);

            metadata.SeriesType.Should().Be(expected);
        }

        [Test]
        public void should_map_volume_number_from_volume_not_issue_count()
        {
            var series = GivenSeries();
            series.VolumeNumber = 2;
            series.IssueCount = 66;

            var (metadata, _) = Subject.MapSeries(series);

            metadata.VolumeNumber.Should().Be(2);
        }

        [TestCase("0.5", "0.5")]
        [TestCase("1a", "1a")]
        [TestCase("3", "3")]
        [TestCase(null, "")]
        public void should_preserve_issue_number_strings(string issueNumber, string expected)
        {
            var issue = Subject.MapIssue(new ProviderIssue
            {
                ForeignIssueId = "1234",
                IssueNumber = issueNumber
            }, 1);

            issue.IssueNumber.Should().Be(expected);
        }
    }

    [TestFixture]
    public class IssueNumberNormalizerFixture
    {
        [TestCase("003", "3")]
        [TestCase("3", "3")]
        [TestCase("0.5", "0.5")]
        [TestCase("0.50", "0.5")]
        [TestCase(" 16 ", "16")]
        [TestCase("1a", "1a")]
        [TestCase("1.MU", "1.MU")]
        [TestCase("½", "½")]
        public void should_normalize_numeric_and_preserve_alpha(string input, string expected)
        {
            IssueNumberNormalizer.Normalize(input).Should().Be(expected);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void should_return_null_for_blank(string input)
        {
            IssueNumberNormalizer.Normalize(input).Should().BeNull();
        }
    }
}
