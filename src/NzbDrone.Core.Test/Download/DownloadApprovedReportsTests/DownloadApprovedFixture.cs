using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download.DownloadApprovedReportsTests
{
    [TestFixture]
    public class DownloadApprovedFixture : CoreTest<ProcessDownloadDecisions>
    {
        [SetUp]
        public void SetUp()
        {
            Mocker.GetMock<IPrioritizeDownloadDecision>()
                .Setup(v => v.PrioritizeDecisions(It.IsAny<List<DownloadDecision>>()))
                .Returns<List<DownloadDecision>>(v => v);
        }

        private Issue GetIssue(int id)
        {
            return Builder<Issue>.CreateNew()
                            .With(e => e.Id = id)
                            .Build();
        }

        private RemoteIssue GetRemoteIssue(List<Issue> issues, QualityModel quality, DownloadProtocol downloadProtocol = DownloadProtocol.Usenet)
        {
            var remoteIssue = new RemoteIssue();
            remoteIssue.ParsedIssueInfo = new ParsedIssueInfo();
            remoteIssue.ParsedIssueInfo.Quality = quality;

            remoteIssue.Issues = new List<Issue>();
            remoteIssue.Issues.AddRange(issues);

            remoteIssue.Release = new ReleaseInfo();
            remoteIssue.Release.DownloadProtocol = downloadProtocol;
            remoteIssue.Release.PublishDate = DateTime.UtcNow;

            remoteIssue.Series = Builder<Series>.CreateNew()
                .With(e => e.QualityProfile = new QualityProfile { Items = Qualities.QualityFixture.GetDefaultQualities() })
                .Build();

            return remoteIssue;
        }

        [Test]
        public async Task should_download_report_if_book_was_not_already_downloaded()
        {
            var issues = new List<Issue> { GetIssue(1) };
            var remoteIssue = GetRemoteIssue(issues, new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteIssue>(), null), Times.Once());
        }

        [Test]
        public async Task should_only_download_book_once()
        {
            var issues = new List<Issue> { GetIssue(1) };
            var remoteIssue = GetRemoteIssue(issues, new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue));
            decisions.Add(new DownloadDecision(remoteIssue));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteIssue>(), null), Times.Once());
        }

        [Test]
        public async Task should_not_download_if_any_book_was_already_downloaded()
        {
            var remoteIssue1 = GetRemoteIssue(
                                                    new List<Issue> { GetIssue(1) },
                                                    new QualityModel(Quality.CBR));

            var remoteIssue2 = GetRemoteIssue(
                                                    new List<Issue> { GetIssue(1), GetIssue(2) },
                                                    new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteIssue>(), null), Times.Once());
        }

        [Test]
        public async Task should_return_downloaded_reports()
        {
            var issues = new List<Issue> { GetIssue(1) };
            var remoteIssue = GetRemoteIssue(issues, new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue));

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().HaveCount(1);
        }

        [Test]
        public async Task should_return_all_downloaded_reports()
        {
            var remoteIssue1 = GetRemoteIssue(
                                                    new List<Issue> { GetIssue(1) },
                                                    new QualityModel(Quality.CBR));

            var remoteIssue2 = GetRemoteIssue(
                                                    new List<Issue> { GetIssue(2) },
                                                    new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().HaveCount(2);
        }

        [Test]
        public async Task should_only_return_downloaded_reports()
        {
            var remoteIssue1 = GetRemoteIssue(
                                                    new List<Issue> { GetIssue(1) },
                                                    new QualityModel(Quality.CBR));

            var remoteIssue2 = GetRemoteIssue(
                                                    new List<Issue> { GetIssue(2) },
                                                    new QualityModel(Quality.CBR));

            var remoteIssue3 = GetRemoteIssue(
                                                    new List<Issue> { GetIssue(2) },
                                                    new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue1));
            decisions.Add(new DownloadDecision(remoteIssue2));
            decisions.Add(new DownloadDecision(remoteIssue3));

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().HaveCount(2);
        }

        [Test]
        public async Task should_not_add_to_downloaded_list_when_download_fails()
        {
            var issues = new List<Issue> { GetIssue(1) };
            var remoteIssue = GetRemoteIssue(issues, new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue));

            Mocker.GetMock<IDownloadService>().Setup(s => s.DownloadReport(It.IsAny<RemoteIssue>(), null)).Throws(new Exception());

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().BeEmpty();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_return_an_empty_list_when_none_are_appproved()
        {
            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(new RemoteIssue(), new Rejection("Failure!")));
            decisions.Add(new DownloadDecision(new RemoteIssue(), new Rejection("Failure!")));

            Subject.GetQualifiedReports(decisions).Should().BeEmpty();
        }

        [Test]
        public async Task should_not_grab_if_pending()
        {
            var issues = new List<Issue> { GetIssue(1) };
            var remoteIssue = GetRemoteIssue(issues, new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue, new Rejection("Failure!", RejectionType.Temporary)));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteIssue>(), null), Times.Never());
        }

        [Test]
        public async Task should_not_add_to_pending_if_book_was_grabbed()
        {
            var issues = new List<Issue> { GetIssue(1) };
            var remoteIssue = GetRemoteIssue(issues, new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue));
            decisions.Add(new DownloadDecision(remoteIssue, new Rejection("Failure!", RejectionType.Temporary)));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IPendingReleaseService>().Verify(v => v.AddMany(It.IsAny<List<Tuple<DownloadDecision, PendingReleaseReason>>>()), Times.Never());
        }

        [Test]
        public async Task should_add_to_pending_even_if_already_added_to_pending()
        {
            var issues = new List<Issue> { GetIssue(1) };
            var remoteIssue = GetRemoteIssue(issues, new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue, new Rejection("Failure!", RejectionType.Temporary)));
            decisions.Add(new DownloadDecision(remoteIssue, new Rejection("Failure!", RejectionType.Temporary)));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IPendingReleaseService>().Verify(v => v.AddMany(It.IsAny<List<Tuple<DownloadDecision, PendingReleaseReason>>>()), Times.Once());
        }

        [Test]
        public async Task should_add_to_failed_if_already_failed_for_that_protocol()
        {
            var issues = new List<Issue> { GetIssue(1) };
            var remoteIssue = GetRemoteIssue(issues, new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue));
            decisions.Add(new DownloadDecision(remoteIssue));

            Mocker.GetMock<IDownloadService>().Setup(s => s.DownloadReport(It.IsAny<RemoteIssue>(), null))
                  .Throws(new DownloadClientUnavailableException("Download client failed"));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteIssue>(), null), Times.Once());
        }

        [Test]
        public async Task should_not_add_to_failed_if_failed_for_a_different_protocol()
        {
            var issues = new List<Issue> { GetIssue(1) };
            var remoteIssue = GetRemoteIssue(issues, new QualityModel(Quality.CBR), DownloadProtocol.Usenet);
            var remoteIssue2 = GetRemoteIssue(issues, new QualityModel(Quality.CBR), DownloadProtocol.Torrent);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue));
            decisions.Add(new DownloadDecision(remoteIssue2));

            Mocker.GetMock<IDownloadService>().Setup(s => s.DownloadReport(It.Is<RemoteIssue>(r => r.Release.DownloadProtocol == DownloadProtocol.Usenet), null))
                  .Throws(new DownloadClientUnavailableException("Download client failed"));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.Is<RemoteIssue>(r => r.Release.DownloadProtocol == DownloadProtocol.Usenet), null), Times.Once());
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.Is<RemoteIssue>(r => r.Release.DownloadProtocol == DownloadProtocol.Torrent), null), Times.Once());
        }

        [Test]
        public async Task should_add_to_rejected_if_release_unavailable_on_indexer()
        {
            var issues = new List<Issue> { GetIssue(1) };
            var remoteIssue = GetRemoteIssue(issues, new QualityModel(Quality.CBR));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteIssue));

            Mocker.GetMock<IDownloadService>()
                  .Setup(s => s.DownloadReport(It.IsAny<RemoteIssue>(), null))
                  .Throws(new ReleaseUnavailableException(remoteIssue.Release, "That 404 Error is not just a Quirk"));

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().BeEmpty();
            result.Rejected.Should().NotBeEmpty();

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
