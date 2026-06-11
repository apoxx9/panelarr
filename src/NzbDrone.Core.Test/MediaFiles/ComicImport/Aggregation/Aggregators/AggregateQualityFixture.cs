using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.IssueImport.Aggregation.Aggregators;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.ComicImport.Aggregation.Aggregators
{
    [TestFixture]
    public class AggregateQualityFixture : CoreTest<AggregateQuality>
    {
        [Test]
        public void should_use_file_tag_quality_when_known()
        {
            var localIssue = new LocalIssue
            {
                Path = @"C:\comics\Saga 003.cbz".AsOsAgnostic(),
                FileTagInfo = new ParsedFileTagInfo { Quality = new QualityModel(Quality.CBR) }
            };

            Subject.Aggregate(localIssue, false).Quality.Quality.Should().Be(Quality.CBR);
        }

        [Test]
        public void should_not_let_unknown_folder_quality_mask_extension()
        {
            // Comic release names usually carry no format token, so the folder
            // title parses to a non-null Unknown quality.
            var localIssue = new LocalIssue
            {
                Path = @"C:\comics\Saga 003 (2012) (Digital) (Zone-Empire).cbz".AsOsAgnostic(),
                FolderTrackInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.Unknown) }
            };

            Subject.Aggregate(localIssue, false).Quality.Quality.Should().Be(Quality.CBZ);
        }

        [Test]
        public void should_not_let_unknown_client_quality_mask_extension()
        {
            var localIssue = new LocalIssue
            {
                Path = @"C:\comics\Saga 003.cbr".AsOsAgnostic(),
                DownloadClientIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.Unknown) }
            };

            Subject.Aggregate(localIssue, false).Quality.Quality.Should().Be(Quality.CBR);
        }

        [Test]
        public void should_fall_back_to_extension_when_no_parsed_info()
        {
            var localIssue = new LocalIssue
            {
                Path = @"C:\comics\Saga 003.cb7".AsOsAgnostic()
            };

            Subject.Aggregate(localIssue, false).Quality.Quality.Should().Be(Quality.CB7);
        }

        [Test]
        public void should_be_unknown_when_nothing_detects()
        {
            var localIssue = new LocalIssue
            {
                Path = @"C:\comics\Saga 003.xyz".AsOsAgnostic(),
                FolderTrackInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.Unknown) }
            };

            Subject.Aggregate(localIssue, false).Quality.Quality.Should().Be(Quality.Unknown);
        }
    }
}
