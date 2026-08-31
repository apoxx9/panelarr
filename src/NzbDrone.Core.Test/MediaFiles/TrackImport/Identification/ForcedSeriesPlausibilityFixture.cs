using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.IssueImport.Identification;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.IssueImport.Identification
{
    [TestFixture]
    public class ForcedSeriesPlausibilityFixture : CoreTest
    {
        private Series _series;

        [SetUp]
        public void Setup()
        {
            _series = new Series
            {
                Name = "Supergirl Annual",
                Path = @"C:\comics\DC Comics\Supergirl Annual (2016)".AsOsAgnostic()
            };
        }

        private static LocalEdition Edition(string path, ParsedFileTagInfo tags = null, ParsedIssueInfo downloadInfo = null)
        {
            return new LocalEdition(new List<LocalIssue>
            {
                new LocalIssue
                {
                    Path = path,
                    FileTagInfo = tags ?? new ParsedFileTagInfo(),
                    DownloadClientIssueInfo = downloadInfo
                }
            });
        }

        [Test]
        public void should_refuse_a_file_from_another_series_folder_whose_title_disagrees()
        {
            // Observed live: a series-forced scan turned "Convergence - Justice
            // League of America 001" into Supergirl Annual #1 - the lone
            // candidate matched on the issue number alone
            var path = @"C:\comics\DC Comics\Convergence Justice League of America (2015)\Convergence - Justice League of America 001 (2015).cbr".AsOsAgnostic();
            var tags = new ParsedFileTagInfo { SeriesTitle = "Convergence: Justice League of America", CleanTitle = "convergencejusticeleagueofamerica" };

            IdentificationService.ForcedSeriesIsPlausible(Edition(path, tags), _series).Should().BeFalse();
        }

        [Test]
        public void should_refuse_an_untagged_file_from_another_series_folder()
        {
            var path = @"C:\comics\DC Comics\Convergence Justice League of America (2015)\Convergence - Justice League of America 001 (2015).cbr".AsOsAgnostic();

            IdentificationService.ForcedSeriesIsPlausible(Edition(path), _series).Should().BeFalse();
        }

        [Test]
        public void should_trust_a_file_inside_the_series_folder()
        {
            var path = @"C:\comics\DC Comics\Supergirl Annual (2016)\untagged 001.cbz".AsOsAgnostic();

            IdentificationService.ForcedSeriesIsPlausible(Edition(path), _series).Should().BeTrue();
        }

        [Test]
        public void should_trust_a_file_delivered_by_a_grab_for_the_series()
        {
            // direct downloads without ComicInfo.xml are the case the
            // force-accept exists for
            var path = @"C:\downloads\Supergirl Annual 001 (2017).cbz".AsOsAgnostic();

            IdentificationService.ForcedSeriesIsPlausible(Edition(path, null, new ParsedIssueInfo { SeriesName = "Supergirl Annual" }), _series).Should().BeTrue();
        }

        [Test]
        public void should_trust_a_file_whose_tagged_series_title_matches()
        {
            var path = @"C:\comics\DC Comics\Supergirl (2016)\Supergirl Annual #01 (2017).cbz".AsOsAgnostic();
            var tags = new ParsedFileTagInfo { SeriesTitle = "Supergirl Annual" };

            IdentificationService.ForcedSeriesIsPlausible(Edition(path, tags), _series).Should().BeTrue();
        }

        [Test]
        public void should_trust_a_file_whose_parsed_folder_series_matches()
        {
            var path = @"C:\comics\DC Comics\Supergirl (2016)\Supergirl Annual #01 (2017).cbz".AsOsAgnostic();
            var edition = Edition(path);
            edition.LocalIssues[0].FolderTrackInfo = new ParsedIssueInfo { SeriesName = "Supergirl Annual" };

            IdentificationService.ForcedSeriesIsPlausible(edition, _series).Should().BeTrue();
        }

        [Test]
        public void should_refuse_when_no_series_is_forced()
        {
            IdentificationService.ForcedSeriesIsPlausible(Edition(@"C:\x\y.cbz".AsOsAgnostic()), null).Should().BeFalse();
        }
    }
}
