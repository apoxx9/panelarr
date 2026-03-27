using System;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class ShouldRefreshSeriesFixture : TestBase<ShouldRefreshSeries>
    {
        private Series _author;

        [SetUp]
        public void Setup()
        {
            _author = Builder<Series>.CreateNew()
                                     .With(v => v.Metadata.Value.Status == SeriesStatusType.Continuing)
                                     .Build();

            Mocker.GetMock<IBookService>()
                  .Setup(s => s.GetBooksBySeries(_author.Id))
                  .Returns(Builder<Issue>.CreateListOfSize(2)
                                           .All()
                                           .With(e => e.ReleaseDate = DateTime.Today.AddDays(-100))
                                           .Build()
                                           .ToList());
        }

        private void GivenSeriesIsEnded()
        {
            _author.Metadata.Value.Status = SeriesStatusType.Ended;
        }

        private void GivenSeriesLastRefreshedMonthsAgo()
        {
            _author.LastInfoSync = DateTime.UtcNow.AddDays(-90);
        }

        private void GivenSeriesLastRefreshedYesterday()
        {
            _author.LastInfoSync = DateTime.UtcNow.AddDays(-1);
        }

        private void GivenSeriesLastRefreshedThreeDaysAgo()
        {
            _author.LastInfoSync = DateTime.UtcNow.AddDays(-3);
        }

        private void GivenSeriesLastRefreshedRecently()
        {
            _author.LastInfoSync = DateTime.UtcNow.AddHours(-7);
        }

        private void GivenRecentlyAired()
        {
            Mocker.GetMock<IBookService>()
                              .Setup(s => s.GetBooksBySeries(_author.Id))
                              .Returns(Builder<Issue>.CreateListOfSize(2)
                                                       .TheFirst(1)
                                                       .With(e => e.ReleaseDate = DateTime.Today.AddDays(-7))
                                                       .TheLast(1)
                                                       .With(e => e.ReleaseDate = DateTime.Today.AddDays(-100))
                                                       .Build()
                                                       .ToList());
        }

        [Test]
        public void should_return_true_if_running_author_last_refreshed_more_than_24_hours_ago()
        {
            GivenSeriesLastRefreshedThreeDaysAgo();

            Subject.ShouldRefresh(_author).Should().BeTrue();
        }

        [Test]
        public void should_return_false_if_running_author_last_refreshed_less_than_12_hours_ago()
        {
            GivenSeriesLastRefreshedRecently();

            Subject.ShouldRefresh(_author).Should().BeFalse();
        }

        [Test]
        public void should_return_false_if_ended_author_last_refreshed_yesterday()
        {
            GivenSeriesIsEnded();
            GivenSeriesLastRefreshedYesterday();

            Subject.ShouldRefresh(_author).Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_author_last_refreshed_more_than_30_days_ago()
        {
            GivenSeriesIsEnded();
            GivenSeriesLastRefreshedMonthsAgo();

            Subject.ShouldRefresh(_author).Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_book_released_in_last_30_days()
        {
            GivenSeriesIsEnded();
            GivenSeriesLastRefreshedYesterday();

            GivenRecentlyAired();

            Subject.ShouldRefresh(_author).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_recently_refreshed_ended_show_has_not_aired_for_30_days()
        {
            GivenSeriesIsEnded();
            GivenSeriesLastRefreshedYesterday();

            Subject.ShouldRefresh(_author).Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_recently_refreshed_ended_show_aired_in_last_30_days()
        {
            GivenSeriesIsEnded();
            GivenSeriesLastRefreshedRecently();

            GivenRecentlyAired();

            Subject.ShouldRefresh(_author).Should().BeFalse();
        }
    }
}
