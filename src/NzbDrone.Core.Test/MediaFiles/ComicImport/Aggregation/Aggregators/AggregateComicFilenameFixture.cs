using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.IssueImport.Aggregation.Aggregators;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.ComicImport.Aggregation.Aggregators
{
    [TestFixture]
    public class AggregateComicFilenameFixture : CoreTest<AggregateComicFilename>
    {
        private LocalIssue GivenUntaggedFile(string fileName)
        {
            return new LocalIssue
            {
                Path = ($@"C:\downloads\" + fileName).AsOsAgnostic(),
                FileTagInfo = new ParsedFileTagInfo()
            };
        }

        [Test]
        public void collected_edition_gets_marker_stripped_series_variant_and_volume_as_issue_number()
        {
            // Real MAM release name that failed to import: the library series
            // is "Iron Fist Epic Collection: The Fury of Iron Fist" with a
            // single issue numbered 1.
            var local = GivenUntaggedFile("Iron Fist Epic Collection Vol. 01 - The Fury of Iron Fist (2015) (digital) (Minutemen-Slayer).cbr");

            var result = Subject.Aggregate(local, false);

            result.FileTagInfo.Series.Should().Contain("Iron Fist Epic Collection Vol. 01 - The Fury of Iron Fist");
            result.FileTagInfo.Series.Should().Contain("Iron Fist Epic Collection The Fury of Iron Fist");
            result.FileTagInfo.SeriesIndex.Should().Be("1");
        }

        [Test]
        public void collected_edition_volume_number_does_not_override_a_parsed_issue_number()
        {
            var local = GivenUntaggedFile("Angel & Faith Season 10 #025 (2016) (digital).cbz");

            var result = Subject.Aggregate(local, false);

            result.FileTagInfo.SeriesIndex.Should().Be("25");
        }

        [Test]
        public void plain_issue_filename_gets_no_collected_edition_variants()
        {
            var local = GivenUntaggedFile("Saga 003 (2012) (Digital) (Zone-Empire).cbz");

            var result = Subject.Aggregate(local, false);

            result.FileTagInfo.Series.Should().NotContain(x => x.Contains("Vol"));
        }

        [Test]
        public void embedded_tags_are_left_alone()
        {
            var local = new LocalIssue
            {
                Path = @"C:\downloads\Iron Fist Epic Collection Vol. 01 - The Fury of Iron Fist (2015).cbr".AsOsAgnostic(),
                FileTagInfo = new ParsedFileTagInfo
                {
                    SeriesTitle = "Iron Fist Epic Collection: The Fury of Iron Fist",
                    IssueTitle = "Volume 1",
                    SeriesIndex = "1"
                }
            };

            var result = Subject.Aggregate(local, false);

            result.FileTagInfo.SeriesTitle.Should().Be("Iron Fist Epic Collection: The Fury of Iron Fist");
            result.FileTagInfo.Series.Should().BeEmpty();
        }
    }
}
