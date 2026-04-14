using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]

    public class ProperSpecificationFixture : CoreTest<ProperSpecification>
    {
        private RemoteIssue _parseResultMulti;
        private RemoteIssue _parseResultSingle;
        private ComicFile _firstFile;
        private ComicFile _secondFile;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();

            _firstFile = new ComicFile { Quality = new QualityModel(Quality.CBZ_HD, new Revision(version: 1)), DateAdded = DateTime.Now };
            _secondFile = new ComicFile { Quality = new QualityModel(Quality.CBZ_HD, new Revision(version: 1)), DateAdded = DateTime.Now };

            var singleIssueList = new List<Issue> { new Issue { }, new Issue { } };
            var doubleIssueList = new List<Issue> { new Issue { }, new Issue { }, new Issue { } };

            var fakeSeries = Builder<Series>.CreateNew()
                         .With(c => c.QualityProfile = new QualityProfile { Cutoff = Quality.CBZ_HD.Id })
                         .Build();

            Mocker.GetMock<IMediaFileService>()
                .Setup(c => c.GetFilesByIssue(It.IsAny<int>()))
                .Returns(new List<ComicFile> { _firstFile, _secondFile });

            _parseResultMulti = new RemoteIssue
            {
                Series = fakeSeries,
                ParsedIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.CBR, new Revision(version: 2)) },
                Issues = doubleIssueList
            };

            _parseResultSingle = new RemoteIssue
            {
                Series = fakeSeries,
                ParsedIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.CBR, new Revision(version: 2)) },
                Issues = singleIssueList
            };
        }

        private void WithFirstFileUpgradable()
        {
            _firstFile.Quality = new QualityModel(Quality.PDF);
        }

        [Test]
        public void should_return_false_when_trackFile_was_added_more_than_7_days_ago()
        {
            _firstFile.Quality.Quality = Quality.CBR;

            _firstFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_first_trackFile_was_added_more_than_7_days_ago()
        {
            _firstFile.Quality.Quality = Quality.CBR;
            _secondFile.Quality.Quality = Quality.CBR;

            _firstFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_second_trackFile_was_added_more_than_7_days_ago()
        {
            _firstFile.Quality.Quality = Quality.CBR;
            _secondFile.Quality.Quality = Quality.CBR;

            _secondFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_trackFile_was_added_more_than_7_days_ago_but_proper_is_for_better_quality()
        {
            WithFirstFileUpgradable();

            _firstFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_trackFile_was_added_more_than_7_days_ago_but_is_for_search()
        {
            WithFirstFileUpgradable();

            _firstFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultSingle, new IssueSearchCriteria()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_proper_but_auto_download_propers_is_false()
        {
            Mocker.GetMock<IConfigService>()
                .Setup(s => s.DownloadPropersAndRepacks)
                .Returns(ProperDownloadTypes.DoNotUpgrade);

            _firstFile.Quality.Quality = Quality.CBR;

            _firstFile.DateAdded = DateTime.Today;
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_trackFile_was_added_today()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.PreferAndUpgrade);

            _firstFile.Quality.Quality = Quality.CBR;

            _firstFile.DateAdded = DateTime.Today;
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_propers_are_not_preferred()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            _firstFile.Quality.Quality = Quality.CBR;

            _firstFile.DateAdded = DateTime.Today;
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }
    }
}
