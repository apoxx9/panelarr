using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class PrioritizeDownloadDecisionFixture : CoreTest<DownloadDecisionPriorizationService>
    {
        [SetUp]
        public void Setup()
        {
            GivenPreferredDownloadProtocol(DownloadProtocol.Usenet);
        }

        private Issue GivenIssue(int id)
        {
            return Builder<Issue>.CreateNew()
                            .With(e => e.Id = id)
                            .Build();
        }

        private RemoteIssue GivenRemoteIssue(List<Issue> issues, QualityModel quality, int age = 0, long size = 0, DownloadProtocol downloadProtocol = DownloadProtocol.Usenet, int indexerPriority = 25)
        {
            var remoteIssue = new RemoteIssue();
            remoteIssue.ParsedIssueInfo = new ParsedIssueInfo();
            remoteIssue.ParsedIssueInfo.Quality = quality;

            remoteIssue.Issues = new List<Issue>();
            remoteIssue.Issues.AddRange(issues);

            remoteIssue.Release = new ReleaseInfo();
            remoteIssue.Release.PublishDate = DateTime.Now.AddDays(-age);
            remoteIssue.Release.Size = size;
            remoteIssue.Release.DownloadProtocol = downloadProtocol;
            remoteIssue.Release.IndexerPriority = indexerPriority;

            remoteIssue.Series = Builder<Series>.CreateNew()
                                                .With(e => e.QualityProfile = new QualityProfile
                                                {
                                                    Items = Qualities.QualityFixture.GetDefaultQualities()
                                                }).Build();

            remoteIssue.DownloadAllowed = true;

            return remoteIssue;
        }

        private void GivenPreferredDownloadProtocol(DownloadProtocol downloadProtocol)
        {
            Mocker.GetMock<IDelayProfileService>()
                  .Setup(s => s.BestForTags(It.IsAny<HashSet<int>>()))
                  .Returns(new DelayProfile
                  {
                      PreferredProtocol = downloadProtocol
                  });
        }

        [Test]
        public void should_put_reals_before_non_reals()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR, new Revision(version: 1, real: 0)));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR, new Revision(version: 1, real: 1)));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Quality.Revision.Real.Should().Be(1);
        }

        [Test]
        public void should_put_propers_before_non_propers()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR, new Revision(version: 1)));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR, new Revision(version: 2)));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_put_higher_quality_before_lower()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Quality.Quality.Should().Be(Quality.CBR);
        }

        [Test]
        public void should_order_by_age_then_largest_rounded_to_200mb()
        {
            var remoteIssueSd = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR), size: 100.Megabytes(), age: 1);
            var remoteIssueHdSmallOld = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR), size: 1200.Megabytes(), age: 1000);
            var remoteIssueSmallYoung = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR), size: 1250.Megabytes(), age: 10);
            var remoteIssueHdLargeYoung = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR), size: 3000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssueSd));
            decisions.Add(new DownloadDecision(remoteIssueHdSmallOld));
            decisions.Add(new DownloadDecision(remoteIssueSmallYoung));
            decisions.Add(new DownloadDecision(remoteIssueHdLargeYoung));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.Should().Be(remoteIssueHdLargeYoung);
        }

        [Test]
        public void should_order_by_youngest()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR), age: 10);
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR), age: 5);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.Should().Be(remoteIssue2);
        }

        [Test]
        public void should_not_throw_if_no_books_are_found()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR), size: 500.Megabytes());
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR), size: 500.Megabytes());

            remoteIssue1.Issues = new List<Issue>();

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            Subject.PrioritizeDecisions(decisions);
        }

        [Test]
        public void should_put_usenet_above_torrent_when_usenet_is_preferred()
        {
            GivenPreferredDownloadProtocol(DownloadProtocol.Usenet);

            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR), downloadProtocol: DownloadProtocol.Torrent);
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR), downloadProtocol: DownloadProtocol.Usenet);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.Release.DownloadProtocol.Should().Be(DownloadProtocol.Usenet);
        }

        [Test]
        public void should_put_torrent_above_usenet_when_torrent_is_preferred()
        {
            GivenPreferredDownloadProtocol(DownloadProtocol.Torrent);

            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR), downloadProtocol: DownloadProtocol.Torrent);
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR), downloadProtocol: DownloadProtocol.Usenet);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.Release.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
        }

        [Test]
        public void should_prefer_discography_pack_above_single_book()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1), GivenIssue(2) }, new QualityModel(Quality.CBZ_HD));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ_HD));

            remoteIssue1.ParsedIssueInfo.Discography = true;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Discography.Should().BeTrue();
        }

        [Test]
        public void should_prefer_quality_over_discography_pack()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1), GivenIssue(2) }, new QualityModel(Quality.CBR));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ_HD));

            remoteIssue1.ParsedIssueInfo.Discography = true;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Discography.Should().BeFalse();
        }

        [Test]
        public void should_prefer_single_book_over_multi_book()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1), GivenIssue(2) }, new QualityModel(Quality.CBR));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.Issues.Count.Should().Be(remoteIssue2.Issues.Count);
        }

        [Test]
        public void should_prefer_releases_with_more_seeders()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 100;

            remoteIssue1.Release = torrentInfo1;
            remoteIssue2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteIssue.Release).Seeders.Should().Be(torrentInfo2.Seeders);
        }

        [Test]
        public void should_prefer_releases_with_more_peers_given_equal_number_of_seeds()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 10;
            torrentInfo1.Peers = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Peers = 100;

            remoteIssue1.Release = torrentInfo1;
            remoteIssue2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteIssue.Release).Peers.Should().Be(torrentInfo2.Peers);
        }

        [Test]
        public void should_prefer_releases_with_more_peers_no_seeds()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 0;
            torrentInfo1.Peers = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 0;
            torrentInfo2.Peers = 100;

            remoteIssue1.Release = torrentInfo1;
            remoteIssue2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteIssue.Release).Peers.Should().Be(torrentInfo2.Peers);
        }

        [Test]
        public void should_prefer_first_release_if_peers_and_size_are_too_similar()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 1000;
            torrentInfo1.Peers = 10;
            torrentInfo1.Size = 200.Megabytes();

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 1100;
            torrentInfo2.Peers = 10;
            torrentInfo1.Size = 250.Megabytes();

            remoteIssue1.Release = torrentInfo1;
            remoteIssue2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteIssue.Release).Should().Be(torrentInfo1);
        }

        [Test]
        public void should_prefer_first_release_if_age_and_size_are_too_similar()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));

            remoteIssue1.Release.PublishDate = DateTime.UtcNow.AddDays(-100);
            remoteIssue1.Release.Size = 200.Megabytes();

            remoteIssue2.Release.PublishDate = DateTime.UtcNow.AddDays(-150);
            remoteIssue2.Release.Size = 250.Megabytes();

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.Release.Should().Be(remoteIssue1.Release);
        }

        [Test]
        public void should_prefer_quality_over_the_number_of_peers()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.DownloadProtocol = DownloadProtocol.Torrent;
            torrentInfo1.Seeders = 100;
            torrentInfo1.Peers = 10;
            torrentInfo1.Size = 200.Megabytes();

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 1100;
            torrentInfo2.Peers = 10;
            torrentInfo1.Size = 250.Megabytes();

            remoteIssue1.Release = torrentInfo1;
            remoteIssue2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteIssue.Release).Should().Be(torrentInfo2);
        }

        [Test]
        public void should_put_higher_quality_before_lower_always()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Quality.Quality.Should().Be(Quality.CBR);
        }

        [Test]
        public void should_prefer_higher_score_over_lower_score()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ_HD));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ_HD));

            remoteIssue1.CustomFormatScore = 10;
            remoteIssue2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.CustomFormatScore.Should().Be(10);
        }

        [Test]
        public void should_prefer_proper_over_score_when_download_propers_is_prefer_and_upgrade()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.PreferAndUpgrade);

            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ_HD, new Revision(1)));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ_HD, new Revision(2)));

            remoteIssue1.CustomFormatScore = 10;
            remoteIssue2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_prefer_proper_over_score_when_download_propers_is_do_not_upgrade()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotUpgrade);

            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ_HD, new Revision(1)));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ_HD, new Revision(2)));

            remoteIssue1.CustomFormatScore = 10;
            remoteIssue2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_prefer_score_over_proper_when_download_propers_is_do_not_prefer()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ_HD, new Revision(1)));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ_HD, new Revision(2)));

            remoteIssue1.CustomFormatScore = 10;
            remoteIssue2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Quality.Quality.Should().Be(Quality.CBZ_HD);
            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Quality.Revision.Version.Should().Be(1);
            qualifiedReports.First().RemoteIssue.CustomFormatScore.Should().Be(10);
        }

        [Test]
        public void sort_download_decisions_based_on_indexer_priority()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ, new Revision(1)), indexerPriority: 25);
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ, new Revision(1)), indexerPriority: 50);
            var remoteIssue3 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ, new Revision(1)), indexerPriority: 1);

            var decisions = new List<DownloadDecision>();
            decisions.AddRange(new[] { new DownloadDecision(remoteIssue1), new DownloadDecision(remoteIssue2), new DownloadDecision(remoteIssue3) });

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.Should().Be(remoteIssue3);
            qualifiedReports.Skip(1).First().RemoteIssue.Should().Be(remoteIssue1);
            qualifiedReports.Last().RemoteIssue.Should().Be(remoteIssue2);
        }

        [Test]
        public void ensure_download_decisions_indexer_priority_is_not_perfered_over_quality()
        {
            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.EPUB, new Revision(1)), indexerPriority: 25);
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ, new Revision(1)), indexerPriority: 50);
            var remoteIssue3 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.PDF, new Revision(1)), indexerPriority: 1);
            var remoteIssue4 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ, new Revision(1)), indexerPriority: 25);

            var decisions = new List<DownloadDecision>();
            decisions.AddRange(new[] { new DownloadDecision(remoteIssue1), new DownloadDecision(remoteIssue2), new DownloadDecision(remoteIssue3), new DownloadDecision(remoteIssue4) });

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteIssue.Should().Be(remoteIssue4);
            qualifiedReports.Skip(1).First().RemoteIssue.Should().Be(remoteIssue2);
            qualifiedReports.Skip(2).First().RemoteIssue.Should().Be(remoteIssue1);
            qualifiedReports.Last().RemoteIssue.Should().Be(remoteIssue3);
        }

        [Test]
        public void should_prefer_score_over_real_when_download_propers_is_do_not_prefer()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            var remoteIssue1 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ_HD, new Revision(1, 0)));
            var remoteIssue2 = GivenRemoteIssue(new List<Issue> { GivenIssue(1) }, new QualityModel(Quality.CBZ_HD, new Revision(1, 1)));

            remoteIssue1.CustomFormatScore = 10;
            remoteIssue2.CustomFormatScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);

            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Quality.Quality.Should().Be(Quality.CBZ_HD);
            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Quality.Revision.Version.Should().Be(1);
            qualifiedReports.First().RemoteIssue.ParsedIssueInfo.Quality.Revision.Real.Should().Be(0);
            qualifiedReports.First().RemoteIssue.CustomFormatScore.Should().Be(10);
        }
    }
}
