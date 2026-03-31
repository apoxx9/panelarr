using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]
    public class DelaySpecificationFixture : CoreTest<DelaySpecification>
    {
        private QualityProfile _profile;
        private DelayProfile _delayProfile;
        private RemoteIssue _remoteIssue;

        [SetUp]
        public void Setup()
        {
            _profile = Builder<QualityProfile>.CreateNew()
                                       .Build();

            _delayProfile = Builder<DelayProfile>.CreateNew()
                                      .With(d => d.PreferredProtocol = DownloadProtocol.Usenet)
                                      .Build();

            var series = Builder<Series>.CreateNew()
                                        .With(s => s.QualityProfile = _profile)
                                        .Build();

            _remoteIssue = Builder<RemoteIssue>.CreateNew()
                                                   .With(r => r.Series = series)
                                                   .Build();

            _profile.Items = new List<QualityProfileQualityItem>();
            _profile.Items.Add(new QualityProfileQualityItem { Allowed = true, Quality = Quality.PDF });
            _profile.Items.Add(new QualityProfileQualityItem { Allowed = true, Quality = Quality.CBZ });
            _profile.Items.Add(new QualityProfileQualityItem { Allowed = true, Quality = Quality.CBR });

            _profile.Cutoff = Quality.CBZ.Id;

            _remoteIssue.ParsedIssueInfo = new ParsedIssueInfo();
            _remoteIssue.Release = new ReleaseInfo();
            _remoteIssue.Release.DownloadProtocol = DownloadProtocol.Usenet;

            _remoteIssue.Issues = Builder<Issue>.CreateListOfSize(1).Build().ToList();

            Mocker.GetMock<IMediaFileService>()
                .Setup(s => s.GetFilesByIssue(It.IsAny<int>()))
                .Returns(new List<ComicFile> { });

            Mocker.GetMock<IDelayProfileService>()
                  .Setup(s => s.BestForTags(It.IsAny<HashSet<int>>()))
                  .Returns(_delayProfile);

            Mocker.GetMock<IPendingReleaseService>()
                  .Setup(s => s.GetPendingRemoteIssues(It.IsAny<int>()))
                  .Returns(new List<RemoteIssue>());
        }

        private void GivenExistingFile(QualityModel quality)
        {
            Mocker.GetMock<IMediaFileService>()
                .Setup(s => s.GetFilesByIssue(It.IsAny<int>()))
                .Returns(new List<ComicFile>
                {
                    new ComicFile
                    {
                        Quality = quality
                    }
                });
        }

        private void GivenUpgradeForExistingFile()
        {
            Mocker.GetMock<IUpgradableSpecification>()
                  .Setup(s => s.IsUpgradable(It.IsAny<QualityProfile>(), It.IsAny<QualityModel>(), It.IsAny<List<CustomFormat>>(), It.IsAny<QualityModel>(), It.IsAny<List<CustomFormat>>()))
                  .Returns(true);
        }

        [Test]
        public void should_be_true_when_user_invoked_search()
        {
            Subject.IsSatisfiedBy(new RemoteIssue(), new IssueSearchCriteria { UserInvokedSearch = true }).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_system_invoked_search_and_release_is_younger_than_delay()
        {
            _remoteIssue.ParsedIssueInfo.Quality = new QualityModel(Quality.CBR);
            _remoteIssue.Release.PublishDate = DateTime.UtcNow;

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteIssue, new IssueSearchCriteria()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_when_profile_does_not_have_a_delay()
        {
            _delayProfile.UsenetDelay = 0;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_quality_is_last_allowed_in_profile_and_bypass_disabled()
        {
            _remoteIssue.Release.PublishDate = DateTime.UtcNow;
            _remoteIssue.ParsedIssueInfo.Quality = new QualityModel(Quality.CBR);

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_when_quality_is_last_allowed_in_profile_and_bypass_enabled()
        {
            _delayProfile.UsenetDelay = 720;
            _delayProfile.BypassIfHighestQuality = true;

            _remoteIssue.Release.PublishDate = DateTime.UtcNow;
            _remoteIssue.ParsedIssueInfo.Quality = new QualityModel(Quality.CBR);

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_true_when_release_is_older_than_delay()
        {
            _remoteIssue.ParsedIssueInfo.Quality = new QualityModel(Quality.CBR);
            _remoteIssue.Release.PublishDate = DateTime.UtcNow.AddHours(-10);

            _delayProfile.UsenetDelay = 60;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_release_is_younger_than_delay()
        {
            _remoteIssue.ParsedIssueInfo.Quality = new QualityModel(Quality.CBR);
            _remoteIssue.Release.PublishDate = DateTime.UtcNow;

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_when_release_is_a_proper_for_existing_book()
        {
            _remoteIssue.ParsedIssueInfo.Quality = new QualityModel(Quality.CBR, new Revision(version: 2));
            _remoteIssue.Release.PublishDate = DateTime.UtcNow;

            GivenExistingFile(new QualityModel(Quality.CBR));
            GivenUpgradeForExistingFile();

            Mocker.GetMock<IUpgradableSpecification>()
                  .Setup(s => s.IsRevisionUpgrade(It.IsAny<QualityModel>(), It.IsAny<QualityModel>()))
                  .Returns(true);

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_true_when_release_is_a_real_for_existing_book()
        {
            _remoteIssue.ParsedIssueInfo.Quality = new QualityModel(Quality.CBR, new Revision(real: 1));
            _remoteIssue.Release.PublishDate = DateTime.UtcNow;

            GivenExistingFile(new QualityModel(Quality.CBR));
            GivenUpgradeForExistingFile();

            Mocker.GetMock<IUpgradableSpecification>()
                  .Setup(s => s.IsRevisionUpgrade(It.IsAny<QualityModel>(), It.IsAny<QualityModel>()))
                  .Returns(true);

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_release_is_proper_for_existing_book_of_different_quality()
        {
            _remoteIssue.ParsedIssueInfo.Quality = new QualityModel(Quality.CBZ, new Revision(version: 2));
            _remoteIssue.Release.PublishDate = DateTime.UtcNow;

            GivenExistingFile(new QualityModel(Quality.PDF));

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_false_when_custom_format_score_is_above_minimum_but_bypass_disabled()
        {
            _remoteIssue.Release.PublishDate = DateTime.UtcNow;
            _remoteIssue.CustomFormatScore = 100;

            _delayProfile.UsenetDelay = 720;
            _delayProfile.MinimumCustomFormatScore = 50;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_false_when_custom_format_score_is_above_minimum_and_bypass_enabled_but_under_minimum()
        {
            _remoteIssue.Release.PublishDate = DateTime.UtcNow;
            _remoteIssue.CustomFormatScore = 5;

            _delayProfile.UsenetDelay = 720;
            _delayProfile.BypassIfAboveCustomFormatScore = true;
            _delayProfile.MinimumCustomFormatScore = 50;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_when_custom_format_score_is_above_minimum_and_bypass_enabled()
        {
            _remoteIssue.Release.PublishDate = DateTime.UtcNow;
            _remoteIssue.CustomFormatScore = 100;

            _delayProfile.UsenetDelay = 720;
            _delayProfile.BypassIfAboveCustomFormatScore = true;
            _delayProfile.MinimumCustomFormatScore = 50;

            Subject.IsSatisfiedBy(_remoteIssue, null).Accepted.Should().BeTrue();
        }
    }
}
