using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Pending;
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
        private Series _author;
        private Issue _book;
        private QualityProfile _profile;
        private ReleaseInfo _release;
        private ParsedBookInfo _parsedBookInfo;
        private RemoteBook _remoteBook;
        private List<PendingRelease> _heldReleases;

        [SetUp]
        public void Setup()
        {
            _author = Builder<Series>.CreateNew()
                                     .Build();

            _book = Builder<Issue>.CreateNew()
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

            _author.QualityProfile = new LazyLoaded<QualityProfile>(_profile);

            _release = Builder<ReleaseInfo>.CreateNew().Build();

            _parsedBookInfo = Builder<ParsedBookInfo>.CreateNew().Build();
            _parsedBookInfo.Quality = new QualityModel(Quality.CBR);

            _remoteBook = new RemoteBook();
            _remoteBook.Books = new List<Issue> { _book };
            _remoteBook.Series = _author;
            _remoteBook.ParsedBookInfo = _parsedBookInfo;
            _remoteBook.Release = _release;

            _temporarilyRejected = new DownloadDecision(_remoteBook, new Rejection("Temp Rejected", RejectionType.Temporary));

            _heldReleases = new List<PendingRelease>();

            Mocker.GetMock<IPendingReleaseRepository>()
                  .Setup(s => s.All())
                  .Returns(_heldReleases);

            Mocker.GetMock<IPendingReleaseRepository>()
                  .Setup(s => s.AllBySeriesId(It.IsAny<int>()))
                  .Returns<int>(i => _heldReleases.Where(v => v.SeriesId == i).ToList());

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(It.IsAny<int>()))
                  .Returns(_author);

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeriess(It.IsAny<IEnumerable<int>>()))
                  .Returns(new List<Series> { _author });

            Mocker.GetMock<IParsingService>()
                  .Setup(s => s.GetBooks(It.IsAny<ParsedBookInfo>(), _author, null))
                  .Returns(new List<Issue> { _book });

            Mocker.GetMock<IPrioritizeDownloadDecision>()
                  .Setup(s => s.PrioritizeDecisions(It.IsAny<List<DownloadDecision>>()))
                  .Returns((List<DownloadDecision> d) => d);
        }

        private void GivenHeldRelease(QualityModel quality)
        {
            var parsedEpisodeInfo = _parsedBookInfo.JsonClone();
            parsedEpisodeInfo.Quality = quality;

            var heldReleases = Builder<PendingRelease>.CreateListOfSize(1)
                                                   .All()
                                                   .With(h => h.SeriesId = _author.Id)
                                                   .With(h => h.Release = _release.JsonClone())
                                                   .With(h => h.ParsedBookInfo = parsedEpisodeInfo)
                                                   .Build();

            _heldReleases.AddRange(heldReleases);
        }

        [Test]
        public void should_delete_if_the_grabbed_quality_is_the_same()
        {
            GivenHeldRelease(_parsedBookInfo.Quality);

            Subject.Handle(new IssueGrabbedEvent(_remoteBook));

            VerifyDelete();
        }

        [Test]
        public void should_delete_if_the_grabbed_quality_is_the_higher()
        {
            GivenHeldRelease(new QualityModel(Quality.CBR));

            Subject.Handle(new IssueGrabbedEvent(_remoteBook));

            VerifyDelete();
        }

        [Test]
        public void should_not_delete_if_the_grabbed_quality_is_the_lower()
        {
            GivenHeldRelease(new QualityModel(Quality.CBZ_HD));

            Subject.Handle(new IssueGrabbedEvent(_remoteBook));

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
