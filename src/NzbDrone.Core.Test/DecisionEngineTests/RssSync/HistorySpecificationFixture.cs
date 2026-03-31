using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.History;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]
    public class HistorySpecificationFixture : CoreTest<HistorySpecification>
    {
        private const int FIRST_ALBUM_ID = 1;
        private const int SECOND_ALBUM_ID = 2;

        private HistorySpecification _upgradeHistory;

        private RemoteIssue _parseResultMulti;
        private RemoteIssue _parseResultSingle;
        private QualityModel _upgradableQuality;
        private QualityModel _notupgradableQuality;
        private Series _fakeSeries;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();
            _upgradeHistory = Mocker.Resolve<HistorySpecification>();

            CustomFormatsTestHelpers.GivenCustomFormats();

            var singleIssueList = new List<Issue> { new Issue { Id = FIRST_ALBUM_ID } };
            var doubleIssueList = new List<Issue>
            {
                                                            new Issue { Id = FIRST_ALBUM_ID },
                                                            new Issue { Id = SECOND_ALBUM_ID },
                                                            new Issue { Id = 3 }
            };

            _fakeSeries = Builder<Series>.CreateNew()
                .With(c => c.QualityProfile = new QualityProfile
                {
                    UpgradeAllowed = true,
                    Cutoff = Quality.CBR.Id,
                    FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems("None"),
                    MinFormatScore = 0,
                    Items = Qualities.QualityFixture.GetDefaultQualities()
                })
                .Build();

            _parseResultMulti = new RemoteIssue
            {
                Series = _fakeSeries,
                ParsedIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.CBR, new Revision(version: 2)) },
                Issues = doubleIssueList,
                CustomFormats = new List<CustomFormat>()
            };

            _parseResultSingle = new RemoteIssue
            {
                Series = _fakeSeries,
                ParsedIssueInfo = new ParsedIssueInfo { Quality = new QualityModel(Quality.CBR, new Revision(version: 2)) },
                Issues = singleIssueList,
                CustomFormats = new List<CustomFormat>()
            };

            _upgradableQuality = new QualityModel(Quality.CBR, new Revision(version: 1));
            _notupgradableQuality = new QualityModel(Quality.CBR, new Revision(version: 2));

            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.EnableCompletedDownloadHandling)
                  .Returns(true);

            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Setup(x => x.ParseCustomFormat(It.IsAny<EntityHistory>(), It.IsAny<Series>()))
                  .Returns(new List<CustomFormat>());
        }

        private void GivenMostRecentForIssue(int issueId, string downloadId, QualityModel quality, DateTime date, EntityHistoryEventType eventType)
        {
            Mocker.GetMock<IHistoryService>().Setup(s => s.MostRecentForIssue(issueId))
                  .Returns(new EntityHistory { DownloadId = downloadId, Quality = quality, Date = date, EventType = eventType });
        }

        private void GivenCdhDisabled()
        {
            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.EnableCompletedDownloadHandling)
                  .Returns(false);
        }

        [Test]
        public void should_return_true_if_it_is_a_search()
        {
            _upgradeHistory.IsSatisfiedBy(_parseResultMulti, new IssueSearchCriteria()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_latest_history_item_is_null()
        {
            Mocker.GetMock<IHistoryService>().Setup(s => s.MostRecentForIssue(It.IsAny<int>())).Returns((EntityHistory)null);
            _upgradeHistory.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_latest_history_item_is_not_grabbed()
        {
            GivenMostRecentForIssue(FIRST_ALBUM_ID, string.Empty, _notupgradableQuality, DateTime.UtcNow, EntityHistoryEventType.DownloadFailed);
            _upgradeHistory.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeTrue();
        }

        //        [Test]
        //        public void should_return_true_if_latest_history_has_a_download_id_and_cdh_is_enabled()
        //        {
        //            GivenMostRecentForEpisode(FIRST_EPISODE_ID, "test", _notupgradableQuality, DateTime.UtcNow, EpisodeHistoryEventType.Grabbed);
        //            _upgradeHistory.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeTrue();
        //        }
        [Test]
        public void should_return_true_if_latest_history_item_is_older_than_twelve_hours()
        {
            GivenMostRecentForIssue(FIRST_ALBUM_ID, string.Empty, _notupgradableQuality, DateTime.UtcNow.AddHours(-12).AddMilliseconds(-1), EntityHistoryEventType.Grabbed);
            _upgradeHistory.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_upgradable_if_only_book_is_upgradable()
        {
            GivenMostRecentForIssue(FIRST_ALBUM_ID, string.Empty, _upgradableQuality, DateTime.UtcNow, EntityHistoryEventType.Grabbed);
            _upgradeHistory.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_upgradable_if_both_books_are_upgradable()
        {
            GivenMostRecentForIssue(FIRST_ALBUM_ID, string.Empty, _upgradableQuality, DateTime.UtcNow, EntityHistoryEventType.Grabbed);
            GivenMostRecentForIssue(SECOND_ALBUM_ID, string.Empty, _upgradableQuality, DateTime.UtcNow, EntityHistoryEventType.Grabbed);
            _upgradeHistory.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_not_be_upgradable_if_both_books_are_not_upgradable()
        {
            GivenMostRecentForIssue(FIRST_ALBUM_ID, string.Empty, _notupgradableQuality, DateTime.UtcNow, EntityHistoryEventType.Grabbed);
            GivenMostRecentForIssue(SECOND_ALBUM_ID, string.Empty, _notupgradableQuality, DateTime.UtcNow, EntityHistoryEventType.Grabbed);
            _upgradeHistory.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_not_upgradable_if_only_first_books_is_upgradable()
        {
            GivenMostRecentForIssue(FIRST_ALBUM_ID, string.Empty, _upgradableQuality, DateTime.UtcNow, EntityHistoryEventType.Grabbed);
            GivenMostRecentForIssue(FIRST_ALBUM_ID, string.Empty, _notupgradableQuality, DateTime.UtcNow, EntityHistoryEventType.Grabbed);
            _upgradeHistory.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_not_upgradable_if_only_second_books_is_upgradable()
        {
            GivenMostRecentForIssue(FIRST_ALBUM_ID, string.Empty, _notupgradableQuality, DateTime.UtcNow, EntityHistoryEventType.Grabbed);
            GivenMostRecentForIssue(SECOND_ALBUM_ID, string.Empty, _upgradableQuality, DateTime.UtcNow, EntityHistoryEventType.Grabbed);
            _upgradeHistory.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_not_be_upgradable_if_book_is_of_same_quality_as_existing()
        {
            _fakeSeries.QualityProfile = new QualityProfile { Cutoff = Quality.CBR.Id, Items = Qualities.QualityFixture.GetDefaultQualities() };
            _parseResultSingle.ParsedIssueInfo.Quality = new QualityModel(Quality.CBR, new Revision(version: 1));
            _upgradableQuality = new QualityModel(Quality.CBR, new Revision(version: 1));

            GivenMostRecentForIssue(FIRST_ALBUM_ID, string.Empty, _upgradableQuality, DateTime.UtcNow, EntityHistoryEventType.Grabbed);

            _upgradeHistory.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_not_be_upgradable_if_cutoff_already_met()
        {
            _fakeSeries.QualityProfile = new QualityProfile { Cutoff = Quality.CBR.Id, Items = Qualities.QualityFixture.GetDefaultQualities() };
            _parseResultSingle.ParsedIssueInfo.Quality = new QualityModel(Quality.CBR, new Revision(version: 1));
            _upgradableQuality = new QualityModel(Quality.CBR, new Revision(version: 1));

            GivenMostRecentForIssue(FIRST_ALBUM_ID, string.Empty, _upgradableQuality, DateTime.UtcNow, EntityHistoryEventType.Grabbed);

            _upgradeHistory.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_latest_history_item_is_only_one_hour_old()
        {
            GivenMostRecentForIssue(FIRST_ALBUM_ID, string.Empty, _notupgradableQuality, DateTime.UtcNow.AddHours(-1), EntityHistoryEventType.Grabbed);
            _upgradeHistory.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_latest_history_has_a_download_id_and_cdh_is_disabled()
        {
            GivenCdhDisabled();
            GivenMostRecentForIssue(FIRST_ALBUM_ID, "test", _upgradableQuality, DateTime.UtcNow.AddDays(-100), EntityHistoryEventType.Grabbed);
            _upgradeHistory.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_cutoff_already_met_and_cdh_is_disabled()
        {
            GivenCdhDisabled();
            _fakeSeries.QualityProfile = new QualityProfile { Cutoff = Quality.CBR.Id, Items = Qualities.QualityFixture.GetDefaultQualities() };
            _parseResultSingle.ParsedIssueInfo.Quality = new QualityModel(Quality.CBR, new Revision(version: 1));
            _upgradableQuality = new QualityModel(Quality.CBR, new Revision(version: 1));

            GivenMostRecentForIssue(FIRST_ALBUM_ID, "test", _upgradableQuality, DateTime.UtcNow.AddDays(-100), EntityHistoryEventType.Grabbed);

            _upgradeHistory.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_only_book_is_not_upgradable_and_cdh_is_disabled()
        {
            GivenCdhDisabled();
            GivenMostRecentForIssue(FIRST_ALBUM_ID, "test", _notupgradableQuality, DateTime.UtcNow.AddDays(-100), EntityHistoryEventType.Grabbed);
            _upgradeHistory.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }
    }
}
