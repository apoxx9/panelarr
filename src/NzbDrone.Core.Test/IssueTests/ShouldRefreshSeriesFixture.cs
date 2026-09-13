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
        public void ended_series_refreshes_monthly()
        {
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Ended, TimeSpan.FromDays(20))).Should().BeFalse();
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Ended, TimeSpan.FromDays(31))).Should().BeTrue();
        }

        [Test]
        public void cancelled_is_treated_as_ended()
        {
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Cancelled, TimeSpan.FromDays(20))).Should().BeFalse();
            Subject.ShouldRefresh(GivenSeries(SeriesStatusType.Cancelled, TimeSpan.FromDays(31))).Should().BeTrue();
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
        public void ended_series_with_an_old_release_waits_for_the_monthly_pass()
        {
            var series = GivenSeries(SeriesStatusType.Ended, TimeSpan.FromDays(3), DateTime.UtcNow.AddDays(-400));

            Subject.ShouldRefresh(series).Should().BeFalse();
        }

        private static Series EndedSeries(int id, TimeSpan sinceSync, SeriesStatusType status = SeriesStatusType.Ended)
        {
            return new Series
            {
                Id = id,
                Name = $"Series {id}",
                LastInfoSync = DateTime.UtcNow - sinceSync,
                Metadata = new SeriesMetadata { Status = status }
            };
        }

        [Test]
        public void budget_takes_the_oldest_due_ended_series_at_the_steady_state_rate()
        {
            // 90 ended series, all due at once (the converged-stamp herd):
            // 90 / 30 days = 3 per run, the three stalest first
            var tier = new List<Series>();

            for (var i = 1; i <= 90; i++)
            {
                tier.Add(EndedSeries(i, TimeSpan.FromDays(31 + i)));
            }

            var budgeted = ShouldRefreshSeries.BudgetEndedTier(tier);

            budgeted.Should().BeEquivalentTo(new[] { 90, 89, 88 });
        }

        [Test]
        public void budget_never_contains_active_or_not_yet_due_series()
        {
            var all = new List<Series>
            {
                EndedSeries(1, TimeSpan.FromDays(40)),
                EndedSeries(2, TimeSpan.FromDays(10)),
                EndedSeries(3, TimeSpan.FromDays(40), SeriesStatusType.Continuing)
            };

            var budgeted = ShouldRefreshSeries.BudgetEndedTier(all);

            budgeted.Should().BeEquivalentTo(new[] { 1 });
        }

        [Test]
        public void budget_is_at_least_one_for_a_small_tier()
        {
            var all = new List<Series> { EndedSeries(1, TimeSpan.FromDays(40)) };

            ShouldRefreshSeries.BudgetEndedTier(all).Should().BeEquivalentTo(new[] { 1 });
        }

        [Test]
        public void budget_is_empty_when_nothing_is_due()
        {
            var all = new List<Series> { EndedSeries(1, TimeSpan.FromDays(5)), EndedSeries(2, TimeSpan.FromDays(6)) };

            ShouldRefreshSeries.BudgetEndedTier(all).Should().BeEmpty();
        }
    }
}
