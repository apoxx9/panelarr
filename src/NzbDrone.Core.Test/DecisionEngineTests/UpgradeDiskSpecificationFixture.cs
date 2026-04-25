using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    [Ignore("Pending Panelarr fixes")]
    public class UpgradeDiskSpecificationFixture : CoreTest<UpgradeDiskSpecification>
    {
        private RemoteIssue _parseResultMulti;
        private RemoteIssue _parseResultSingle;
        private ComicFile _firstFile;
        private ComicFile _secondFile;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();

            CustomFormatsTestHelpers.GivenCustomFormats();

            _firstFile = new ComicFile { Quality = new QualityModel(Quality.CBZ_HD, new Revision(version: 2)), DateAdded = DateTime.Now };
            _secondFile = new ComicFile { Quality = new QualityModel(Quality.CBZ_HD, new Revision(version: 2)), DateAdded = DateTime.Now };

            var singleIssueList = new List<Issue> { new Issue { ComicFiles = new List<ComicFile>() } };
            var doubleIssueList = new List<Issue> { new Issue { ComicFiles = new List<ComicFile>() }, new Issue { ComicFiles = new List<ComicFile>() }, new Issue { ComicFiles = new List<ComicFile>() } };

            var fakeSeries = Builder<Series>.CreateNew()
                         .With(c => c.QualityProfile = new QualityProfile
                         {
                             UpgradeAllowed = true,
                             Cutoff = Quality.CBR.Id,
                             Items = Qualities.QualityFixture.GetDefaultQualities(),
                             FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems("None"),
                             MinFormatScore = 0,
                         })
                         .Build();

            Mocker.GetMock<IMediaFileService>()
                  .Setup(c => c.GetFilesByIssue(It.IsAny<int>()))
                  .Returns(new List<ComicFile> { _firstFile, _secondFile });

            _parseResultMulti = new RemoteIssue
            {
                Series = fakeSeries,
                ParsedIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.CBR, new Revision(version: 2)) },
                Issues = doubleIssueList,
                CustomFormats = new List<CustomFormat>()
            };

            _parseResultSingle = new RemoteIssue
            {
                Series = fakeSeries,
                ParsedIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.CBR, new Revision(version: 2)) },
                Issues = singleIssueList,
                CustomFormats = new List<CustomFormat>()
            };

            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Setup(x => x.ParseCustomFormat(It.IsAny<ComicFile>()))
                  .Returns(new List<CustomFormat>());
        }

        private void WithFirstFileUpgradable()
        {
            _firstFile.Quality = new QualityModel(Quality.CBR);
        }

        private void WithSecondFileUpgradable()
        {
            _secondFile.Quality = new QualityModel(Quality.CBR);
        }

        [Test]
        public void should_return_true_if_book_has_no_existing_file()
        {
            _parseResultSingle.Issues.First().ComicFiles = new List<ComicFile>();

            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_track_is_missing()
        {
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_only_query_db_for_missing_tracks_once()
        {
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_single_book_doesnt_exist_on_disk()
        {
            _parseResultSingle.Issues = new List<Issue>();

            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_upgradable_if_all_files_are_upgradable()
        {
            WithFirstFileUpgradable();
            WithSecondFileUpgradable();
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_not_be_upgradable_if_qualities_are_the_same()
        {
            _firstFile.Quality = new QualityModel(Quality.CBR);
            _secondFile.Quality = new QualityModel(Quality.CBR);
            _parseResultSingle.ParsedIssueInfo.Quality = new QualityModel(Quality.CBR);
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_not_be_upgradable_if_all_tracks_are_not_upgradable()
        {
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_if_some_tracks_are_upgradable_and_none_are_downgrades()
        {
            WithFirstFileUpgradable();
            _parseResultSingle.ParsedIssueInfo.Quality = _secondFile.Quality;
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_if_some_tracks_are_upgradable_and_some_are_downgrades()
        {
            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Setup(s => s.ParseCustomFormat(It.IsAny<ComicFile>()))
                  .Returns(new List<CustomFormat>());

            WithFirstFileUpgradable();
            _parseResultSingle.ParsedIssueInfo.Quality = new QualityModel(Quality.CBR);
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }
    }
}
