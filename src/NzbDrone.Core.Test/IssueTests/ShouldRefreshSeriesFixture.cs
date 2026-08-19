using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IssueTests
{
    [TestFixture]
    public class ShouldRefreshSeriesFixture : CoreTest<ShouldRefreshSeries>
    {
        private Series GivenSeries(SeriesStatusType status, TimeSpan sinceSync, DateTime? lastRelease = null)
        {
            var series = new Series
            {
                Id = 7,
                Name = "Test",
                LastInfoSync = DateTime.UtcNow - sinceSync,
                Metadata = new SeriesMetadata { Status = status }
            };

            var issues = new List<Issue>();

            if (lastRelease.HasValue)
            {
                issues.Add(new Issue { ReleaseDate = lastRelease.Value });
            }

            Mocker.GetMock<IIssueService>()
                  .Setup(s => s.GetIssuesBySeries(7))
                  .Returns(issues);

            return series;
        }

        [Test]
        public void anything_synced_within_12_hours_is_not_refreshed()
        {
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Continuing, TimeSpan.FromHours(6))).Should().BeFalse();
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Ended, TimeSpan.FromHours(6))).Should().BeFalse();
        }

        [Test]
        public void continuing_series_refreshes_daily()
        {
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Continuing, TimeSpan.FromHours(20))).Should().BeFalse();
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Continuing, TimeSpan.FromHours(25))).Should().BeTrue();
        }

        [Test]
        public void hiatus_counts_as_active()
        {
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Hiatus, TimeSpan.FromHours(25))).Should().BeTrue();
        }

        [Test]
        public void ended_series_refreshes_weekly()
        {
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Ended, TimeSpan.FromDays(3))).Should().BeFalse();
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Ended, TimeSpan.FromDays(8))).Should().BeTrue();
        }

        [Test]
        public void cancelled_is_treated_as_ended()
        {
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Cancelled, TimeSpan.FromDays(3))).Should().BeFalse();
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Cancelled, TimeSpan.FromDays(8))).Should().BeTrue();
        }

        [Test]
        public void ended_series_with_a_recent_release_refreshes_daily()
        {
            // a late entry or mis-flagged "ended" - the recent release is the
            // one signal that the series is still moving
            var series = GivenSeries(SeriesStatusType.Ended, TimeSpan.FromDays(3), DateTime.UtcNow.AddDays(-10));

            Subject.ShouldRefresh(series).Should().BeTrue();
        }

        [Test]
        public void ended_series_with_an_old_release_waits_for_the_weekly_pass()
        {
            var series = GivenSeries(SeriesStatusType.Ended, TimeSpan.FromDays(3), DateTime.UtcNow.AddDays(-400));

            Subject.ShouldRefresh(series).Should().BeFalse();
        }
    }
}
