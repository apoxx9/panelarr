using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.Pending.PendingReleaseServiceTests
{
    [TestFixture]
    public class RemoveGrabbedFixture : CoreTest<PendingReleaseService>
    {
        private DownloadDecision _temporarilyRejected;
        private Series _series;
        private Issue _issue;
        private QualityProfile _profile;
        private ReleaseInfo _release;
        private ParsedIssueInfo _parsedIssueInfo;
        private RemoteIssue _remoteIssue;
        private List<PendingRelease> _heldReleases;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>.CreateNew()
                                     .Build();

            _issue = Builder<Issue>.CreateNew()
                                       .Build();

            _profile = new QualityProfile
            {
                Name = "Test",
                Cutoff = Quality.CBR.Id,
                Items = new List<QualityProfileQualityItem>
                                   {
                                       new QualityProfileQualityItem { Allowed = true, Quality = Quality.CBR },
                                       new QualityProfileQualityItem { Allowed = true, Quality = Quality.CBR },
                                       new QualityProfileQualityItem { Allowed = true, Quality = Quality.CBZ_HD }
                                   },
            };

            _series.QualityProfile = new LazyLoaded<QualityProfile>(_profile);

            _release = Builder<ReleaseInfo>.CreateNew().Build();

            _parsedIssueInfo = Builder<ParsedIssueInfo>.CreateNew().Build();
            _parsedIssueInfo.Quality = new QualityModel(Quality.CBR);

            _remoteIssue = new RemoteIssue();
            _remoteIssue.Issues = new List<Issue> { _issue };
            _remoteIssue.Series = _series;
            _remoteIssue.ParsedIssueInfo = _parsedIssueInfo;
            _remoteIssue.Release = _release;

            _temporarilyRejected = new DownloadDecision(_remoteIssue, new Rejection("Temp Rejected", RejectionType.Temporary));

            _heldReleases = new List<PendingRelease>();

            Mocker.GetMock<IPendingReleaseRepository>()
                  .Setup(s => s.All())
                  .Returns(_heldReleases);

            Mocker.GetMock<IPendingReleaseRepository>()
                  .Setup(s => s.AllBySeriesId(It.IsAny<int>()))
                  .Returns<int>(i => _heldReleases.Where(v => v.SeriesId == i).ToList());

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(It.IsAny<int>()))
                  .Returns(_series);

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Series> { _series });

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetIssues(It.IsAny<ParsedIssueInfo>(), _series, null))
                  .Returns(new List<Issue> { _issue });

            Mocker.GetMock<IPrioritizeDownloadDecision>()
                  .Setup(s => s.PrioritizeDecisions(It.IsAny<List<DownloadDecision>>()))
                  .Returns((List<DownloadDecision> d) => d);
        }

        private void GivenHeldRelease(QualityModel quality)
        {
            var parsedEpisodeInfo = _parsedIssueInfo.JsonClone();
            parsedEpisodeInfo.Quality = quality;

            var heldReleases = Builder<PendingRelease>.CreateListOfSize(1)
                                                   .All()
                                                   .With(h => h.SeriesId = _series.Id)
                                                   .With(h => h.Release = _release.JsonClone())
                                                   .With(h => h.ParsedIssueInfo = parsedEpisodeInfo)
                                                   .Build();

            _heldReleases.AddRange(heldReleases);
        }

        [Test]
        public void should_delete_if_the_grabbed_quality_is_the_same()
        {
            GivenHeldRelease(_parsedIssueInfo.Quality);

            Subject.Handle(new IssueGrabbedEvent(_remoteIssue));

            VerifyDelete();
        }

        [Test]
        public void should_delete_if_the_grabbed_quality_is_the_higher()
        {
            GivenHeldRelease(new QualityModel(Quality.CBR));

            Subject.Handle(new IssueGrabbedEvent(_remoteIssue));

            VerifyDelete();
        }

        [Test]
        public void should_not_delete_if_the_grabbed_quality_is_the_lower()
        {
            GivenHeldRelease(new QualityModel(Quality.CBZ_HD));

            Subject.Handle(new IssueGrabbedEvent(_remoteIssue));

            VerifyNoDelete();
        }

        private void VerifyDelete()
        {
            Mocker.GetMock<IPendingReleaseRepository>()
                .Verify(v => v.Delete(It.IsAny<PendingRelease>()), Times.Once());
        }

        private void VerifyNoDelete()
        {
            Mocker.GetMock<IPendingReleaseRepository>()
                .Verify(v => v.Delete(It.IsAny<PendingRelease>()), Times.Never());
        }
    }
}
