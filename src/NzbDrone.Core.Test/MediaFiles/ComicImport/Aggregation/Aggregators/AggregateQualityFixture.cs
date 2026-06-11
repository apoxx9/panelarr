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
                FileTagInfo = new ParsedFileTagInfo { Quality = new QualityModel(Quality.Scan) }
            };

            Subject.Aggregate(localIssue, false).Quality.Quality.Should().Be(Quality.Scan);
        }

        [Test]
        public void filename_source_tag_should_beat_unknown_folder_quality()
        {
            var localIssue = new LocalIssue
            {
                Path = @"C:\comics\Saga 003 (2012) (Digital) (Zone-Empire).cbz".AsOsAgnostic(),
                FolderTrackInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.Unknown) }
            };

            Subject.Aggregate(localIssue, false).Quality.Quality.Should().Be(Quality.Digital);
        }

        [Test]
        public void untagged_filename_should_not_mask_extension()
        {
            // Untagged comic names parse to a non-null Unknown quality, which
            // must fall through to the extension (an archive of unknown source).
            var localIssue = new LocalIssue
            {
                Path = @"C:\comics\Saga 003 (2012) (Zone-Empire).cbz".AsOsAgnostic(),
                FolderTrackInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.Unknown) }
            };

            Subject.Aggregate(localIssue, false).Quality.Quality.Should().Be(Quality.Archive);
        }

        [Test]
        public void filename_fix_marker_should_survive_extension_fallback()
        {
            var localIssue = new LocalIssue
            {
                Path = @"C:\comics\Saga 003 (2012) (f) (Zone-Empire).cbz".AsOsAgnostic()
            };

            var result = Subject.Aggregate(localIssue, false).Quality;

            result.Quality.Should().Be(Quality.Archive);
            result.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_not_let_unknown_client_quality_mask_extension()
        {
            var localIssue = new LocalIssue
            {
                Path = @"C:\comics\Saga 003.cbr".AsOsAgnostic(),
                DownloadClientIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.Unknown) }
            };

            Subject.Aggregate(localIssue, false).Quality.Quality.Should().Be(Quality.Archive);
        }

        [Test]
        public void should_fall_back_to_extension_when_no_parsed_info()
        {
            var localIssue = new LocalIssue
            {
                Path = @"C:\comics\Saga 003.cb7".AsOsAgnostic()
            };

            Subject.Aggregate(localIssue, false).Quality.Quality.Should().Be(Quality.Archive);
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
